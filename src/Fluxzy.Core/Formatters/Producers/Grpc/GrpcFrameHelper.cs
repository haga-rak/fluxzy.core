// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Formatters.Producers.Grpc
{
    internal static class GrpcFrameHelper
    {
        public static bool IsGrpcContentType(ExchangeInfo exchange)
        {
            var contentType = exchange.GetRequestHeaders()
                .Where(h => h.Name.Span.Equals("content-type", StringComparison.OrdinalIgnoreCase))
                .Select(h => h.Value.ToString())
                .LastOrDefault();

            if (contentType == null)
                return false;

            return contentType.StartsWith("application/grpc", StringComparison.OrdinalIgnoreCase);
        }

        public static bool TryExtractPayload(
            ReadOnlySpan<byte> data, out ReadOnlyMemory<byte> payload, out bool compressed)
        {
            payload = default;
            compressed = false;

            if (data.Length < 5)
                return false;

            compressed = data[0] != 0;
            var length = BinaryPrimitives.ReadUInt32BigEndian(data.Slice(1, 4));

            if (data.Length < 5 + length)
                return false;

            payload = data.Slice(5, (int) length).ToArray();
            return true;
        }

        public static List<GrpcFrame> ExtractFrames(ReadOnlyMemory<byte> body)
        {
            var frames = new List<GrpcFrame>();
            var span = body.Span;
            var offset = 0;

            while (offset + 5 <= span.Length) {
                var compressed = span[offset] != 0;
                var length = (int) BinaryPrimitives.ReadUInt32BigEndian(span.Slice(offset + 1, 4));

                if (offset + 5 + length > span.Length)
                    break;

                var frameData = body.Slice(offset + 5, length);
                frames.Add(new GrpcFrame(frameData, compressed));
                offset += 5 + length;
            }

            return frames;
        }

        public static (string? service, string? method) ExtractGrpcPath(ExchangeInfo exchange)
        {
            var path = exchange.Path;

            if (string.IsNullOrEmpty(path))
                return (null, null);

            // gRPC path format: /package.Service/Method
            var trimmed = path.TrimStart('/');
            var slashIndex = trimmed.IndexOf('/');

            if (slashIndex < 0)
                return (trimmed, null);

            var service = trimmed.Substring(0, slashIndex);
            var method = trimmed.Substring(slashIndex + 1);

            return (service, method);
        }
    }

    internal readonly struct GrpcFrame
    {
        public GrpcFrame(ReadOnlyMemory<byte> data, bool compressed)
        {
            Data = data;
            Compressed = compressed;
        }

        public ReadOnlyMemory<byte> Data { get; }

        public bool Compressed { get; }
    }
}
