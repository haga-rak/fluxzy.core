// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Windows-specific process tracking implementation.
    /// </summary>
    internal static class WindowsProcessHelper
    {
        public static ProcessInfo? GetProcessInfo(int localPort)
        {
            var pid = GetProcessIdFromPort(localPort);

            if (pid == null)
                return null;

            var processPath = GetProcessPath(pid.Value);
            var processArguments = GetProcessArguments(pid.Value);
            return new ProcessInfo(pid.Value, processPath, processArguments);
        }

        private static int? GetProcessIdFromPort(int localPort)
        {
            const int afInet = 2; // AF_INET (IPv4)
            const int tcpTableOwnerPidAll = 5; // TCP_TABLE_OWNER_PID_ALL (listeners + connections)

            // First call to get required buffer size
            var bufferSize = 0;
            var result = WindowsNativeMethods.GetExtendedTcpTable(
                IntPtr.Zero, ref bufferSize, false, afInet, tcpTableOwnerPidAll, 0);

            if (result != WindowsNativeMethods.ErrorInsufficientBuffer && result != WindowsNativeMethods.NoError)
                return null;

            // Rent a buffer from the pool
            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                try
                {
                    var tablePtr = handle.AddrOfPinnedObject();
                    result = WindowsNativeMethods.GetExtendedTcpTable(
                        tablePtr, ref bufferSize, false, afInet, tcpTableOwnerPidAll, 0);

                    if (result != WindowsNativeMethods.NoError)
                        return null;

                    return FindProcessIdInTable(buffer.AsSpan(0, bufferSize), localPort);
                }
                finally
                {
                    handle.Free();
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static int? FindProcessIdInTable(ReadOnlySpan<byte> tableBuffer, int localPort)
        {
            if (tableBuffer.Length < 4)
                return null;

            var numEntries = MemoryMarshal.Read<int>(tableBuffer);

            if (numEntries == 0)
                return null;

            const int rowSize = 24; // Size of MIB_TCPROW_OWNER_PID
            const int headerSize = 4; // dwNumEntries

            var expectedSize = headerSize + (numEntries * rowSize);
            if (tableBuffer.Length < expectedSize)
                return null;

            var rowsSpan = tableBuffer.Slice(headerSize);

            for (var i = 0; i < numEntries; i++)
            {
                var rowSpan = rowsSpan.Slice(i * rowSize, rowSize);

                // MIB_TCPROW_OWNER_PID layout:
                // Offset 0: dwState (4 bytes)
                // Offset 4: dwLocalAddr (4 bytes)
                // Offset 8: dwLocalPort (4 bytes) - network byte order
                // Offset 12: dwRemoteAddr (4 bytes)
                // Offset 16: dwRemotePort (4 bytes)
                // Offset 20: dwOwningPid (4 bytes)

                var portNetworkOrder = MemoryMarshal.Read<int>(rowSpan.Slice(8, 4));
                var port = NetworkToHostPort(portNetworkOrder);

                if (port == localPort)
                {
                    return MemoryMarshal.Read<int>(rowSpan.Slice(20, 4));
                }
            }

            return null;
        }

        internal static int NetworkToHostPort(int networkPort)
        {
            // Port is stored in network byte order (big-endian) in the low 16 bits
            var portBytes = (ushort)(networkPort & 0xFFFF);
            return (ushort)((portBytes >> 8) | (portBytes << 8));
        }

        private static string? GetProcessPath(int processId)
        {
            const int processQueryLimitedInformation = 0x1000;

            var processHandle = WindowsNativeMethods.OpenProcess(
                processQueryLimitedInformation, false, processId);

            if (processHandle == IntPtr.Zero)
                return null;

            try
            {
                var capacity = 1024;
                var pathBuilder = new StringBuilder(capacity);

                if (WindowsNativeMethods.QueryFullProcessImageName(
                    processHandle, 0, pathBuilder, ref capacity))
                {
                    return pathBuilder.ToString();
                }

                return null;
            }
            finally
            {
                WindowsNativeMethods.CloseHandle(processHandle);
            }
        }

        private static string? GetProcessArguments(int processId)
        {
            const int processQueryInformation = 0x0400;
            const int processVmRead = 0x0010;

            var processHandle = WindowsNativeMethods.OpenProcess(
                processQueryInformation | processVmRead, false, processId);

            if (processHandle == IntPtr.Zero)
                return null;

            try
            {
                // Get process basic information to find PEB address
                var pbi = new WindowsNativeMethods.ProcessBasicInformation();
                var status = WindowsNativeMethods.NtQueryInformationProcess(
                    processHandle, 0, ref pbi, Marshal.SizeOf(pbi), out _);

                if (status != 0)
                    return null;

                // Read PEB to get RTL_USER_PROCESS_PARAMETERS address
                var pebBuffer = new byte[IntPtr.Size];
                if (!WindowsNativeMethods.ReadProcessMemory(
                    processHandle,
                    pbi.PebBaseAddress + (Environment.Is64BitProcess ? 0x20 : 0x10),
                    pebBuffer, pebBuffer.Length, out _))
                    return null;

                var processParametersAddress = Environment.Is64BitProcess
                    ? BitConverter.ToInt64(pebBuffer, 0)
                    : BitConverter.ToInt32(pebBuffer, 0);

                // Read CommandLine UNICODE_STRING (offset 0x70 for 64-bit, 0x40 for 32-bit)
                var unicodeStringOffset = Environment.Is64BitProcess ? 0x70 : 0x40;
                var unicodeStringBuffer = new byte[Environment.Is64BitProcess ? 16 : 8];
                if (!WindowsNativeMethods.ReadProcessMemory(
                    processHandle,
                    new IntPtr(processParametersAddress + unicodeStringOffset),
                    unicodeStringBuffer, unicodeStringBuffer.Length, out _))
                    return null;

                // Parse UNICODE_STRING structure (Length, MaxLength, Buffer pointer)
                var length = BitConverter.ToUInt16(unicodeStringBuffer, 0);
                var bufferAddress = Environment.Is64BitProcess
                    ? BitConverter.ToInt64(unicodeStringBuffer, 8)
                    : BitConverter.ToInt32(unicodeStringBuffer, 4);

                if (length == 0 || bufferAddress == 0)
                    return null;

                // Read the actual command line string
                var commandLineBuffer = new byte[length];
                if (!WindowsNativeMethods.ReadProcessMemory(
                    processHandle,
                    new IntPtr(bufferAddress),
                    commandLineBuffer, commandLineBuffer.Length, out _))
                    return null;

                return Encoding.Unicode.GetString(commandLineBuffer);
            }
            catch
            {
                return null;
            }
            finally
            {
                WindowsNativeMethods.CloseHandle(processHandle);
            }
        }
    }
}
