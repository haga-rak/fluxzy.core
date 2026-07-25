// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
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
            // Find the socket inodes for the given port
            var inodes = FindSocketInodesForPort(localPort);

            if (inodes.Count == 0)
                return null;

            // Find the process that owns one of those sockets
            var pid = FindProcessBySocketInodes(inodes);

            if (pid == null)
                return null;

            var processPath = GetProcessPath(pid.Value);
            var processArguments = GetProcessArguments(pid.Value);
            return new ProcessInfo(pid.Value, processPath, processArguments);
        }

        private static HashSet<long> FindSocketInodesForPort(int localPort)
        {
            // Try IPv4 first, then IPv6
            var inodes = FindSocketInodesInFile("/proc/net/tcp", localPort);

            if (inodes.Count == 0)
                inodes = FindSocketInodesInFile("/proc/net/tcp6", localPort);

            return inodes;
        }

        private static HashSet<long> FindSocketInodesInFile(string path, int localPort)
        {
            var inodes = new HashSet<long>();

            if (!File.Exists(path))
                return inodes;

            try
            {
                using var reader = new StreamReader(path);
                CollectSocketInodes(reader, localPort, inodes);
            }
            catch (IOException)
            {
                // partial results are still usable
            }
            catch (UnauthorizedAccessException)
            {
                // partial results are still usable
            }

            return inodes;
        }

        internal static void CollectSocketInodes(TextReader reader, int localPort, HashSet<long> inodes)
        {
            var portHex = localPort.ToString("X4", CultureInfo.InvariantCulture);

            // Skip header line
            reader.ReadLine();

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                var inode = ParseTcpLineForPort(line, portHex);

                // Several entries may share the same local port. Sockets with no owning file
                // descriptor (TIME_WAIT, connections still in the accept queue) report inode 0
                // and must not shadow the live socket we are looking for.
                if (inode is > 0)
                    inodes.Add(inode.Value);
            }
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

        private static int? FindProcessBySocketInodes(HashSet<long> inodes)
        {
            var socketLinks = new HashSet<string>(StringComparer.Ordinal);

            foreach (var inode in inodes)
                socketLinks.Add($"socket:[{inode}]");

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
                                if (linkTarget != null && socketLinks.Contains(linkTarget.Name))
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
