// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Provides process tracking functionality using platform-specific APIs.
    /// Supports Windows, Linux, and macOS.
    /// </summary>
    public sealed class ProcessTracker : IProcessTracker
    {
        /// <summary>
        /// Default instance of the process tracker.
        /// </summary>
        public static readonly ProcessTracker Instance = new();

        public ProcessInfo? GetProcessInfo(int localPort)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetProcessInfoWindows(localPort);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetProcessInfoLinux(localPort);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetProcessInfoMacOs(localPort);

            throw new PlatformNotSupportedException(
                "ProcessTracker is only supported on Windows, Linux, and macOS.");
        }

        #region Windows Implementation

        private static ProcessInfo? GetProcessInfoWindows(int localPort)
        {
            var pid = GetProcessIdFromPortWindows(localPort);

            if (pid == null)
                return null;

            var processPath = GetProcessPathWindows(pid.Value);
            return new ProcessInfo(pid.Value, processPath);
        }

        private static int? GetProcessIdFromPortWindows(int localPort)
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

                    return FindProcessIdInTableWindows(buffer.AsSpan(0, bufferSize), localPort);
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

        private static int? FindProcessIdInTableWindows(ReadOnlySpan<byte> tableBuffer, int localPort)
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

        private static int NetworkToHostPort(int networkPort)
        {
            // Port is stored in network byte order (big-endian) in the low 16 bits
            var portBytes = (ushort)(networkPort & 0xFFFF);
            return (ushort)((portBytes >> 8) | (portBytes << 8));
        }

        private static string? GetProcessPathWindows(int processId)
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

        private static class WindowsNativeMethods
        {
            public const int NoError = 0;
            public const int ErrorInsufficientBuffer = 122;

            [DllImport("iphlpapi.dll", SetLastError = true)]
            public static extern int GetExtendedTcpTable(
                IntPtr pTcpTable,
                ref int pdwSize,
                bool bOrder,
                int ulAf,
                int tableClass,
                int reserved);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenProcess(
                int dwDesiredAccess,
                bool bInheritHandle,
                int dwProcessId);

            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool CloseHandle(IntPtr hObject);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            public static extern bool QueryFullProcessImageName(
                IntPtr hProcess,
                int dwFlags,
                StringBuilder lpExeName,
                ref int lpdwSize);
        }

        #endregion

        #region Linux Implementation

        private static ProcessInfo? GetProcessInfoLinux(int localPort)
        {
            // Find the socket inode for the given port
            var inode = FindSocketInodeForPort(localPort);

            if (inode == null)
                return null;

            // Find the process that owns this socket
            var pid = FindProcessBySocketInode(inode.Value);

            if (pid == null)
                return null;

            var processPath = GetProcessPathLinux(pid.Value);
            return new ProcessInfo(pid.Value, processPath);
        }

        private static long? FindSocketInodeForPort(int localPort)
        {
            // Try IPv4 first, then IPv6
            var inode = FindSocketInodeInFile("/proc/net/tcp", localPort);

            if (inode == null)
                inode = FindSocketInodeInFile("/proc/net/tcp6", localPort);

            return inode;
        }

        private static long? FindSocketInodeInFile(string path, int localPort)
        {
            if (!File.Exists(path))
                return null;

            var portHex = localPort.ToString("X4", CultureInfo.InvariantCulture);

            try
            {
                using var reader = new StreamReader(path);

                // Skip header line
                reader.ReadLine();

                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    var inode = ParseTcpLineForPort(line, portHex);
                    if (inode != null)
                        return inode;
                }
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }

            return null;
        }

        private static long? ParseTcpLineForPort(ReadOnlySpan<char> line, ReadOnlySpan<char> portHex)
        {
            // Format: sl local_address rem_address st tx_queue:rx_queue tr:tm->when retrnsmt uid timeout inode
            // Example: 0: 0100007F:1F90 00000000:0000 0A 00000000:00000000 00:00000000 00000000 1000 0 12345 1 ...

            line = line.Trim();
            if (line.IsEmpty)
                return null;

            // Skip "sl" column (index)
            var colonIndex = line.IndexOf(':');
            if (colonIndex < 0)
                return null;

            line = line.Slice(colonIndex + 1).TrimStart();

            // Parse local_address (IP:PORT)
            var spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0)
                return null;

            var localAddress = line.Slice(0, spaceIndex);
            var localPortColonIndex = localAddress.LastIndexOf(':');
            if (localPortColonIndex < 0)
                return null;

            var localPortSpan = localAddress.Slice(localPortColonIndex + 1);

            // Compare port (case-insensitive hex comparison)
            if (!localPortSpan.Equals(portHex, StringComparison.OrdinalIgnoreCase))
                return null;

            // Found matching port, now extract inode
            // Skip to inode field (field index 9, 0-based)
            line = line.Slice(spaceIndex).TrimStart();

            // Skip rem_address
            spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0) return null;
            line = line.Slice(spaceIndex).TrimStart();

            // Skip st (state)
            spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0) return null;
            line = line.Slice(spaceIndex).TrimStart();

            // Skip tx_queue:rx_queue
            spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0) return null;
            line = line.Slice(spaceIndex).TrimStart();

            // Skip tr:tm->when
            spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0) return null;
            line = line.Slice(spaceIndex).TrimStart();

            // Skip retrnsmt
            spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0) return null;
            line = line.Slice(spaceIndex).TrimStart();

            // Skip uid
            spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0) return null;
            line = line.Slice(spaceIndex).TrimStart();

            // Skip timeout
            spaceIndex = line.IndexOf(' ');
            if (spaceIndex < 0) return null;
            line = line.Slice(spaceIndex).TrimStart();

            // Now we're at inode
            spaceIndex = line.IndexOf(' ');
            var inodeSpan = spaceIndex >= 0 ? line.Slice(0, spaceIndex) : line;

            if (long.TryParse(inodeSpan, NumberStyles.Integer, CultureInfo.InvariantCulture, out var inode))
                return inode;

            return null;
        }

        private static int? FindProcessBySocketInode(long inode)
        {
            var socketLink = $"socket:[{inode}]";

            try
            {
                foreach (var procDir in Directory.EnumerateDirectories("/proc"))
                {
                    var dirName = Path.GetFileName(procDir);

                    // Only process numeric directories (PIDs)
                    if (!int.TryParse(dirName, out var pid))
                        continue;

                    var fdDir = Path.Combine(procDir, "fd");

                    if (!Directory.Exists(fdDir))
                        continue;

                    try
                    {
                        foreach (var fdPath in Directory.EnumerateFiles(fdDir))
                        {
                            try
                            {
                                var linkTarget = File.ResolveLinkTarget(fdPath, false);
                                if (linkTarget != null &&
                                    linkTarget.Name.Equals(socketLink, StringComparison.Ordinal))
                                {
                                    return pid;
                                }
                            }
                            catch (IOException)
                            {
                                // FD may have been closed, continue
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // No permission to read this fd, continue
                            }
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // No permission to read fd directory, continue to next process
                    }
                    catch (DirectoryNotFoundException)
                    {
                        // Process may have exited, continue
                    }
                }
            }
            catch (IOException)
            {
                return null;
            }

            return null;
        }

        private static string? GetProcessPathLinux(int processId)
        {
            var exePath = $"/proc/{processId}/exe";

            try
            {
                var linkTarget = File.ResolveLinkTarget(exePath, true);
                return linkTarget?.FullName;
            }
            catch (IOException)
            {
                return null;
            }
            catch (UnauthorizedAccessException)
            {
                return null;
            }
        }

        #endregion

        #region macOS Implementation

        private static ProcessInfo? GetProcessInfoMacOs(int localPort)
        {
            // Get all PIDs
            var pids = GetAllPidsMacOs();
            if (pids == null)
                return null;

            foreach (var pid in pids)
            {
                if (pid <= 0)
                    continue;

                if (ProcessOwnsPortMacOs(pid, localPort))
                {
                    var processPath = GetProcessPathMacOs(pid);
                    return new ProcessInfo(pid, processPath);
                }
            }

            return null;
        }

        private static int[]? GetAllPidsMacOs()
        {
            // First call to get the number of PIDs
            var bufferSize = MacOsNativeMethods.proc_listpids(
                MacOsNativeMethods.PROC_ALL_PIDS, 0, IntPtr.Zero, 0);

            if (bufferSize <= 0)
                return null;

            var pidCount = bufferSize / sizeof(int);
            var pids = new int[pidCount];

            var handle = GCHandle.Alloc(pids, GCHandleType.Pinned);
            try
            {
                var result = MacOsNativeMethods.proc_listpids(
                    MacOsNativeMethods.PROC_ALL_PIDS, 0, handle.AddrOfPinnedObject(), bufferSize);

                if (result <= 0)
                    return null;

                // Actual number of PIDs may be less
                var actualCount = result / sizeof(int);
                if (actualCount < pidCount)
                    Array.Resize(ref pids, actualCount);

                return pids;
            }
            finally
            {
                handle.Free();
            }
        }

        private static bool ProcessOwnsPortMacOs(int pid, int localPort)
        {
            // Get file descriptor info size
            var bufferSize = MacOsNativeMethods.proc_pidinfo(
                pid, MacOsNativeMethods.PROC_PIDLISTFDS, 0, IntPtr.Zero, 0);

            if (bufferSize <= 0)
                return false;

            var buffer = ArrayPool<byte>.Shared.Rent(bufferSize);

            try
            {
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                try
                {
                    var result = MacOsNativeMethods.proc_pidinfo(
                        pid, MacOsNativeMethods.PROC_PIDLISTFDS, 0,
                        handle.AddrOfPinnedObject(), bufferSize);

                    if (result <= 0)
                        return false;

                    var fdCount = result / MacOsNativeMethods.PROC_FDINFO_SIZE;
                    var bufferSpan = buffer.AsSpan(0, result);

                    for (var i = 0; i < fdCount; i++)
                    {
                        var fdInfoSpan = bufferSpan.Slice(
                            i * MacOsNativeMethods.PROC_FDINFO_SIZE,
                            MacOsNativeMethods.PROC_FDINFO_SIZE);

                        // proc_fdinfo structure:
                        // uint32_t proc_fdtype (offset 0)
                        // int32_t proc_fd (offset 4)
                        var fdType = MemoryMarshal.Read<uint>(fdInfoSpan);
                        var fd = MemoryMarshal.Read<int>(fdInfoSpan.Slice(4));

                        // Check if it's a socket
                        if (fdType != MacOsNativeMethods.PROX_FDTYPE_SOCKET)
                            continue;

                        // Get socket info
                        if (GetSocketLocalPortMacOs(pid, fd) == localPort)
                            return true;
                    }
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

            return false;
        }

        private static int GetSocketLocalPortMacOs(int pid, int fd)
        {
            var buffer = ArrayPool<byte>.Shared.Rent(MacOsNativeMethods.SOCKET_FDINFO_SIZE);

            try
            {
                var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                try
                {
                    var result = MacOsNativeMethods.proc_pidfdinfo(
                        pid, fd, MacOsNativeMethods.PROC_PIDFDSOCKETINFO,
                        handle.AddrOfPinnedObject(), MacOsNativeMethods.SOCKET_FDINFO_SIZE);

                    if (result != MacOsNativeMethods.SOCKET_FDINFO_SIZE)
                        return -1;

                    var bufferSpan = buffer.AsSpan(0, result);

                    // socket_fdinfo structure layout:
                    // proc_fileinfo pfi (offset 0, size 80)
                    // socket_info psi (offset 80)
                    //   - soi_stat (vinfo_stat, offset 80, size 120)
                    //   - soi_so (offset 200, size 8)
                    //   - soi_pcb (offset 208, size 8)
                    //   - soi_type (offset 216, size 4)
                    //   - soi_protocol (offset 220, size 4)
                    //   - soi_family (offset 224, size 4)
                    //   - soi_options (offset 228, size 2)
                    //   - soi_linger (offset 230, size 2)
                    //   - soi_state (offset 232, size 2)
                    //   - soi_qlen (offset 234, size 2)
                    //   - soi_incqlen (offset 236, size 2)
                    //   - soi_qlimit (offset 238, size 2)
                    //   - soi_timeo (offset 240, size 2)
                    //   - soi_error (offset 242, size 2)
                    //   - soi_oobmark (offset 244, size 4)
                    //   - soi_rcv/soi_snd sockbuf_info (offset 248, 256 for each)
                    //   - soi_kind (offset 280, size 4)
                    //   - padding (offset 284, size 4)
                    //   - union pri (offset 288) - for TCP this is pri_tcp (in_sockinfo + tcp_sockinfo)

                    // Check socket family (AF_INET = 2, AF_INET6 = 30)
                    var family = MemoryMarshal.Read<int>(bufferSpan.Slice(224));
                    if (family != 2 && family != 30) // AF_INET or AF_INET6
                        return -1;

                    // Check if TCP (IPPROTO_TCP = 6)
                    var protocol = MemoryMarshal.Read<int>(bufferSpan.Slice(220));
                    if (protocol != 6)
                        return -1;

                    // For TCP sockets, the local port is in the in_sockinfo structure
                    // pri_tcp.tcpsi_ini (in_sockinfo) starts at offset 288
                    // in_sockinfo layout:
                    //   insi_fport (offset 0, 4 bytes) - foreign port
                    //   insi_lport (offset 4, 4 bytes) - local port
                    //   ... rest of structure

                    // Local port is at offset 288 + 4 = 292, stored in network byte order
                    var localPortNetworkOrder = MemoryMarshal.Read<int>(bufferSpan.Slice(292));
                    return NetworkToHostPort(localPortNetworkOrder);
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

        private static string? GetProcessPathMacOs(int pid)
        {
            var buffer = new byte[MacOsNativeMethods.PROC_PIDPATHINFO_MAXSIZE];
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            try
            {
                var result = MacOsNativeMethods.proc_pidpath(
                    pid, handle.AddrOfPinnedObject(), (uint)buffer.Length);

                if (result <= 0)
                    return null;

                return Encoding.UTF8.GetString(buffer, 0, result);
            }
            finally
            {
                handle.Free();
            }
        }

        private static class MacOsNativeMethods
        {
            public const int PROC_ALL_PIDS = 1;
            public const int PROC_PIDLISTFDS = 1;
            public const int PROC_PIDFDSOCKETINFO = 3;
            public const uint PROX_FDTYPE_SOCKET = 2;
            public const int PROC_FDINFO_SIZE = 8; // sizeof(proc_fdinfo)
            public const int SOCKET_FDINFO_SIZE = 808; // sizeof(socket_fdinfo)
            public const int PROC_PIDPATHINFO_MAXSIZE = 4096;

            [DllImport("libproc.dylib")]
            public static extern int proc_listpids(int type, uint typeinfo, IntPtr buffer, int buffersize);

            [DllImport("libproc.dylib")]
            public static extern int proc_pidinfo(int pid, int flavor, ulong arg, IntPtr buffer, int buffersize);

            [DllImport("libproc.dylib")]
            public static extern int proc_pidfdinfo(int pid, int fd, int flavor, IntPtr buffer, int buffersize);

            [DllImport("libproc.dylib")]
            public static extern int proc_pidpath(int pid, IntPtr buffer, uint buffersize);
        }

        #endregion
    }
}
