// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Text;

namespace Fluxzy.Formatters.Producers.Grpc
{
    internal class ResponseProtobufProducer : IFormattingProducer<ProtobufFormattingResult>
    {
        public string ResultTitle => "gRPC Response";

        public ProtobufFormattingResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (!GrpcFrameHelper.IsGrpcContentType(exchangeInfo))
                return null;

            var responseBody = context.ResponseBodyContent;

            if (responseBody == null || responseBody.Length == 0)
                return BuildTrailerOnlyResult(exchangeInfo);

            var maxLength = context.Settings.MaxFormattableProtobufLength;

            if (responseBody.Length > maxLength) {
                return new ProtobufFormattingResult(ResultTitle, "") {
                    ErrorMessage = $"Response body too large to format ({responseBody.Length} bytes)"
                };
            }

            var (serviceName, methodName) = GrpcFrameHelper.ExtractGrpcPath(exchangeInfo);
            var bodyMemory = new ReadOnlyMemory<byte>(responseBody);
            var frames = GrpcFrameHelper.ExtractFrames(bodyMemory);

            if (frames.Count == 0)
                return BuildTrailerOnlyResult(exchangeInfo);

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

                var decoded = RequestProtobufProducer.TryDecodeFrame(
                    frame.Data, registry, serviceName, methodName,
                    false, maxLength, out var usedDescriptor);

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

            PopulateGrpcTrailers(exchangeInfo, result);

            return result;
        }

        private ProtobufFormattingResult? BuildTrailerOnlyResult(ExchangeInfo exchangeInfo)
        {
            var trailers = exchangeInfo.GetResponseTrailers()?.ToList();

            if (trailers == null || trailers.Count == 0)
                return null;

            var result = new ProtobufFormattingResult(ResultTitle, "");
            PopulateGrpcTrailers(exchangeInfo, result);

            // Only return if we found gRPC trailers
            if (result.GrpcStatus.HasValue)
                return result;

            return null;
        }

        private static void PopulateGrpcTrailers(ExchangeInfo exchangeInfo, ProtobufFormattingResult result)
        {
            var trailers = exchangeInfo.GetResponseTrailers()?.ToList();

            if (trailers == null)
                return;

            var statusHeader = trailers
                .FirstOrDefault(h => h.Name.Span.Equals("grpc-status", StringComparison.OrdinalIgnoreCase));

            if (statusHeader != null) {
                var statusStr = statusHeader.Value.ToString();

                if (int.TryParse(statusStr, out var status))
                    result.GrpcStatus = status;
            }

            var messageHeader = trailers
                .FirstOrDefault(h => h.Name.Span.Equals("grpc-message", StringComparison.OrdinalIgnoreCase));

            if (messageHeader != null)
                result.GrpcMessage = messageHeader.Value.ToString();
        }

        private static ProtoFileRegistry? GetRegistry(ProducerContext context)
        {
            if (context.Settings.ProtoDirectories.Count == 0)
                return null;

            return new ProtoFileRegistry(context.Settings.ProtoDirectories);
        }
    }
}
