// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text;

namespace Fluxzy.Formatters.Producers.Grpc
{
    internal class RequestProtobufProducer : IFormattingProducer<ProtobufFormattingResult>
    {
        public string ResultTitle => "gRPC Request";

        public ProtobufFormattingResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (!GrpcFrameHelper.IsGrpcContentType(exchangeInfo))
                return null;

            if (context.RequestBody.IsEmpty)
                return null;

            var maxLength = context.Settings.MaxFormattableProtobufLength;

            if (context.RequestBody.Length > maxLength) {
                return new ProtobufFormattingResult(ResultTitle, "") {
                    ErrorMessage = $"Request body too large to format ({context.RequestBody.Length} bytes)"
                };
            }

            var (serviceName, methodName) = GrpcFrameHelper.ExtractGrpcPath(exchangeInfo);
            var frames = GrpcFrameHelper.ExtractFrames(context.RequestBody);

            if (frames.Count == 0)
                return null;

            var sb = new StringBuilder();
            var hasDescriptor = false;
            var registry = GetRegistry(context);

            for (var i = 0; i < frames.Count; i++) {
                var frame = frames[i];

                if (frames.Count > 1)
                    sb.AppendLine($"--- Frame {i + 1} ---");

                if (frame.Compressed) {
                    sb.AppendLine("[compressed gRPC frame - decoding not supported]");
                    continue;
                }

                var decoded = TryDecodeFrame(frame.Data, registry, serviceName, methodName,
                    true, maxLength, out var usedDescriptor);

                if (usedDescriptor)
                    hasDescriptor = true;

                sb.Append(decoded);

                if (i < frames.Count - 1)
                    sb.AppendLine();
            }

            var result = new ProtobufFormattingResult(ResultTitle, sb.ToString()) {
                ServiceName = serviceName,
                MethodName = methodName,
                HasProtoDescriptor = hasDescriptor
            };

            return result;
        }

        internal static string TryDecodeFrame(
            ReadOnlyMemory<byte> data,
            ProtoFileRegistry? registry,
            string? serviceName,
            string? methodName,
            bool isRequest,
            int maxLength,
            out bool usedDescriptor)
        {
            usedDescriptor = false;

            if (registry != null && serviceName != null && methodName != null) {
                var (input, output) = registry.FindServiceMethod(serviceName, methodName);
                var descriptor = isRequest ? input : output;

                if (descriptor != null) {
                    var protoPath = registry.GetFirstProtoDirectory();
                    var json = DescriptorProtobufDecoder.TryDecode(descriptor, data, protoPath);

                    if (json != null) {
                        usedDescriptor = true;
                        return json;
                    }
                }
            }

            return RawProtobufDecoder.Decode(data.Span, maxLength);
        }

        private static ProtoFileRegistry? GetRegistry(ProducerContext context)
        {
            if (context.Settings.ProtoDirectories.Count == 0)
                return null;

            return new ProtoFileRegistry(context.Settings.ProtoDirectories);
        }
    }
}
