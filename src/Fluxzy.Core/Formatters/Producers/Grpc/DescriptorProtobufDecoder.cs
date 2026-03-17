// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Google.Protobuf.Reflection;

namespace Fluxzy.Formatters.Producers.Grpc
{
    internal static class DescriptorProtobufDecoder
    {
        public static string? TryDecode(
            MessageDescriptor descriptor,
            ReadOnlyMemory<byte> data,
            string? protoPath)
        {
            if (protoPath == null)
                return null;

            try {
                return DecodeWithProtoc(descriptor.FullName, data, protoPath);
            }
            catch {
                return null;
            }
        }

        private static string? DecodeWithProtoc(
            string messageType, ReadOnlyMemory<byte> data, string protoPath)
        {
            var protoFiles = Directory.GetFiles(protoPath, "*.proto", SearchOption.AllDirectories);

            if (protoFiles.Length == 0)
                return null;

            var relativeFiles = protoFiles
                .Select(f => Path.GetRelativePath(protoPath, f))
                .ToArray();

            var args = $"--decode={messageType} --proto_path=\"{protoPath}\" " +
                       string.Join(" ", relativeFiles.Select(f => $"\"{f}\""));

            var psi = new ProcessStartInfo("protoc", args) {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);

            if (process == null)
                return null;

            using (var stdin = process.StandardInput.BaseStream) {
                stdin.Write(data.Span);
            }

            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(10000);

            if (process.ExitCode != 0)
                return null;

            return string.IsNullOrWhiteSpace(output) ? null : output.TrimEnd();
        }
    }
}
