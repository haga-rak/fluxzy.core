// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Formatters.Producers.Grpc
{
    public class ProtobufFormattingResult : FormattingResult
    {
        public ProtobufFormattingResult(string title, string formattedContent)
            : base(title)
        {
            FormattedContent = formattedContent;
        }

        public string FormattedContent { get; }

        public bool HasProtoDescriptor { get; set; }

        public string? ServiceName { get; set; }

        public string? MethodName { get; set; }

        public int? GrpcStatus { get; set; }

        public string? GrpcMessage { get; set; }
    }
}
