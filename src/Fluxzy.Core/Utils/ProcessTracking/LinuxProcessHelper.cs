// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Globalization;
using System.IO;

namespace Fluxzy.Utils.ProcessTracking
{
    /// <summary>
    /// Linux-specific process tracking implementation.
    /// </summary>
    internal static class LinuxProcessHelper
    {
        public static ProcessInfo? GetProcessInfo(int localPort)
        {
            // Find the socket inode for the given port
            var inode = FindSocketInodeForPort(localPort);

            if (inode == null)
                return null;

            // Find the process that owns this socket
            var pid = FindProcessBySocketInode(inode.Value);

            if (pid == null)
                return null;

            var processPath = GetProcessPath(pid.Value);
            var processArguments = GetProcessArguments(pid.Value);
            return new ProcessInfo(pid.Value, processPath, processArguments);
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

        private static string? GetProcessPath(int processId)
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

        private static string? GetProcessArguments(int processId)
        {
            var cmdlinePath = $"/proc/{processId}/cmdline";

            try
            {
                if (!File.Exists(cmdlinePath))
                    return null;

                var content = File.ReadAllText(cmdlinePath);
                if (string.IsNullOrEmpty(content))
                    return null;

                // Arguments are separated by null bytes, replace with spaces
                return content.Replace('\0', ' ').Trim();
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
    }
}
