// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers;
using System.Runtime.InteropServices;
using System.Text;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// macOS-specific process tracking implementation.
    /// </summary>
    internal static class MacOsProcessHelper
    {
        // Byte offsets into struct socket_fdinfo (see GetSocketLocalPort) and the socket constants we
        // filter on. AF_INET / AF_INET6 / IPPROTO_TCP from <sys/socket.h> / <netinet/in.h>.
        private const int ProtocolOffset = 180; // psi.soi_protocol
        private const int FamilyOffset = 184;   // psi.soi_family
        private const int LocalPortOffset = 268; // psi.soi_proto.pri_tcp.tcpsi_ini.insi_lport (net order)
        private const int AfInet = 2;
        private const int AfInet6 = 30;
        private const int IpProtoTcp = 6;

        public static ProcessInfo? GetProcessInfo(int localPort)
        {
            // Get all PIDs
            var pids = GetAllPids();
            if (pids == null)
                return null;

            foreach (var pid in pids)
            {
                if (pid <= 0)
                    continue;

                if (ProcessOwnsPort(pid, localPort))
                {
                    var processPath = GetProcessPath(pid);
                    var processArguments = GetProcessArguments(pid);
                    return new ProcessInfo(pid, processPath, processArguments);
                }
            }

            return null;
        }

        private static int[]? GetAllPids()
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

        private static bool ProcessOwnsPort(int pid, int localPort)
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

                        // struct proc_fdinfo { int32_t proc_fd; uint32_t proc_fdtype; }
                        var fd = MemoryMarshal.Read<int>(fdInfoSpan);
                        var fdType = MemoryMarshal.Read<uint>(fdInfoSpan.Slice(4));

                        // Check if it's a socket
                        if (fdType != MacOsNativeMethods.PROX_FDTYPE_SOCKET)
                            continue;

                        // Get socket info
                        if (GetSocketLocalPort(pid, fd) == localPort)
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

        private static int GetSocketLocalPort(int pid, int fd)
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

                    // proc_pidfdinfo returns the number of bytes written. Require enough to reach the
                    // local-port field rather than an exact struct-size match: struct socket_fdinfo can
                    // grow across OS versions, and an exact compare silently broke the whole lookup.
                    if (result < LocalPortOffset + sizeof(int))
                        return -1;

                    var bufferSpan = buffer.AsSpan(0, result);

                    // Byte offsets into struct socket_fdinfo, LP64 (identical on x86_64 and arm64),
                    // computed from <sys/proc_info.h>: proc_fileinfo pfi (24 bytes) then socket_info psi.
                    // psi.soi_protocol @180, psi.soi_family @184,
                    // psi.soi_proto.pri_tcp.tcpsi_ini.insi_lport @268 (network byte order).
                    var family = MemoryMarshal.Read<int>(bufferSpan.Slice(FamilyOffset));
                    if (family != AfInet && family != AfInet6)
                        return -1;

                    var protocol = MemoryMarshal.Read<int>(bufferSpan.Slice(ProtocolOffset));
                    if (protocol != IpProtoTcp)
                        return -1;

                    var localPortNetworkOrder = MemoryMarshal.Read<int>(bufferSpan.Slice(LocalPortOffset));
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

        private static int NetworkToHostPort(int networkPort)
        {
            // Port is stored in network byte order (big-endian) in the low 16 bits
            var portBytes = (ushort)(networkPort & 0xFFFF);
            return (ushort)((portBytes >> 8) | (portBytes << 8));
        }

        private static string? GetProcessPath(int pid)
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

        private static string? GetProcessArguments(int pid)
        {
            // Use sysctl to get process arguments
            var mib = new int[] { MacOsNativeMethods.CTL_KERN, MacOsNativeMethods.KERN_PROCARGS2, pid };

            // First call to get size
            var size = IntPtr.Zero;
            if (MacOsNativeMethods.sysctl(mib, 3, IntPtr.Zero, ref size, IntPtr.Zero, IntPtr.Zero) != 0)
                return null;

            if (size == IntPtr.Zero || size.ToInt64() <= 0)
                return null;

            var buffer = new byte[size.ToInt64()];
            var bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            try
            {
                var bufferSize = new IntPtr(buffer.Length);
                if (MacOsNativeMethods.sysctl(mib, 3, bufferHandle.AddrOfPinnedObject(), ref bufferSize, IntPtr.Zero, IntPtr.Zero) != 0)
                    return null;

                // Parse KERN_PROCARGS2 format:
                // First 4 bytes: argc (number of arguments)
                // Then: executable path (null-terminated)
                // Then: arguments (null-separated)
                // Then: environment variables (null-separated)

                if (buffer.Length < 4)
                    return null;

                var argc = BitConverter.ToInt32(buffer, 0);
                if (argc <= 0)
                    return null;

                // Find the start of executable path (after argc)
                var offset = 4;

                // Skip any leading nulls
                while (offset < buffer.Length && buffer[offset] == 0)
                    offset++;

                // Skip executable path
                while (offset < buffer.Length && buffer[offset] != 0)
                    offset++;

                // Skip null terminator after executable path
                while (offset < buffer.Length && buffer[offset] == 0)
                    offset++;

                // Now collect arguments
                var args = new StringBuilder();
                var argCount = 0;

                while (offset < buffer.Length && argCount < argc)
                {
                    var argStart = offset;
                    while (offset < buffer.Length && buffer[offset] != 0)
                        offset++;

                    if (offset > argStart)
                    {
                        if (args.Length > 0)
                            args.Append(' ');
                        args.Append(Encoding.UTF8.GetString(buffer, argStart, offset - argStart));
                        argCount++;
                    }

                    offset++; // Skip null terminator
                }

                return args.Length > 0 ? args.ToString() : null;
            }
            finally
            {
                bufferHandle.Free();
            }
        }
    }
}
