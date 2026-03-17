// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Google.Protobuf;
using Google.Protobuf.Reflection;

namespace Fluxzy.Formatters.Producers.Grpc
{
    internal class ProtoFileRegistry
    {
        private readonly List<string> _protoDirectories;
        private readonly object _lock = new();
        private volatile List<FileDescriptor>? _descriptors;
        private volatile bool _initialized;

        public ProtoFileRegistry(List<string> protoDirectories)
        {
            _protoDirectories = protoDirectories;
        }

        public List<string> ProtoDirectories => _protoDirectories;

        private List<FileDescriptor> GetDescriptors()
        {
            if (_initialized)
                return _descriptors ?? new List<FileDescriptor>();

            lock (_lock) {
                if (_initialized)
                    return _descriptors ?? new List<FileDescriptor>();

                _descriptors = CompileDescriptors();
                _initialized = true;

                return _descriptors;
            }
        }

        private List<FileDescriptor> CompileDescriptors()
        {
            var result = new List<FileDescriptor>();

            foreach (var dir in _protoDirectories) {
                if (!Directory.Exists(dir))
                    continue;

                var protoFiles = Directory.GetFiles(dir, "*.proto", SearchOption.AllDirectories);

                if (protoFiles.Length == 0)
                    continue;

                try {
                    var descriptorSet = CompileWithProtoc(dir, protoFiles);

                    if (descriptorSet != null) {
                        var fileDescriptors = BuildFileDescriptors(descriptorSet);
                        result.AddRange(fileDescriptors);
                    }
                }
                catch {
                    // protoc not available or compilation failed - skip silently
                }
            }

            return result;
        }

        private static FileDescriptorSet? CompileWithProtoc(string protoPath, string[] protoFiles)
        {
            var tempFile = Path.GetTempFileName();

            try {
                var relativeFiles = protoFiles
                    .Select(f => Path.GetRelativePath(protoPath, f))
                    .ToArray();

                var args = $"--descriptor_set_out=\"{tempFile}\" --include_imports --proto_path=\"{protoPath}\" " +
                           string.Join(" ", relativeFiles.Select(f => $"\"{f}\""));

                var psi = new ProcessStartInfo("protoc", args) {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);

                if (process == null)
                    return null;

                process.WaitForExit(30000);

                if (process.ExitCode != 0)
                    return null;

                var bytes = File.ReadAllBytes(tempFile);
                return FileDescriptorSet.Parser.ParseFrom(bytes);
            }
            finally {
                try {
                    File.Delete(tempFile);
                }
                catch {
                    // ignore cleanup errors
                }
            }
        }

        private static List<FileDescriptor> BuildFileDescriptors(FileDescriptorSet descriptorSet)
        {
            var byName = new Dictionary<string, FileDescriptor>();
            var result = new List<FileDescriptor>();

            // Collect all byte strings for batch building
            var byteStrings = descriptorSet.File
                .Select(f => f.ToByteString())
                .ToList();

            try {
                var descriptors = FileDescriptor.BuildFromByteStrings(byteStrings);
                result.AddRange(descriptors);
            }
            catch {
                // If batch build fails, try building individually
                foreach (var fileProto in descriptorSet.File) {
                    try {
                        var descriptors = FileDescriptor.BuildFromByteStrings(
                            new[] { fileProto.ToByteString() });

                        if (descriptors.Any()) {
                            var fd = descriptors.Last();
                            byName[fileProto.Name] = fd;
                            result.Add(fd);
                        }
                    }
                    catch {
                        // skip files that can't be built individually
                    }
                }
            }

            return result;
        }

        public MessageDescriptor? FindMessageType(string fullName)
        {
            foreach (var fd in GetDescriptors()) {
                var msg = FindMessageInFile(fd, fullName);

                if (msg != null)
                    return msg;
            }

            return null;
        }

        public (MessageDescriptor? input, MessageDescriptor? output) FindServiceMethod(
            string serviceName, string methodName)
        {
            foreach (var fd in GetDescriptors()) {
                foreach (var service in fd.Services) {
                    if (service.FullName == serviceName || service.Name == serviceName) {
                        var method = service.Methods
                            .FirstOrDefault(m => m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase));

                        if (method != null)
                            return (method.InputType, method.OutputType);
                    }
                }
            }

            return (null, null);
        }

        public string? GetFirstProtoDirectory()
        {
            return _protoDirectories.FirstOrDefault(d => Directory.Exists(d));
        }

        private static MessageDescriptor? FindMessageInFile(FileDescriptor fd, string fullName)
        {
            foreach (var msg in fd.MessageTypes) {
                if (msg.FullName == fullName)
                    return msg;

                var nested = FindNestedMessage(msg, fullName);

                if (nested != null)
                    return nested;
            }

            return null;
        }

        private static MessageDescriptor? FindNestedMessage(MessageDescriptor parent, string fullName)
        {
            foreach (var nested in parent.NestedTypes) {
                if (nested.FullName == fullName)
                    return nested;

                var found = FindNestedMessage(nested, fullName);

                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
