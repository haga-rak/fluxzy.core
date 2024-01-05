// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class RequestBodyAnalysis : IFormattingProducer<RequestBodyAnalysisResult>
    {
        public string ResultTitle => "Body details";

        public RequestBodyAnalysisResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (context.RequestBodyLength == 0)
                return null;

            var contentType = exchangeInfo.GetRequestHeaders()
                                          .Where(h => h.Name.Span.Equals("content-type",
                                              StringComparison.OrdinalIgnoreCase))
                                          .Select(s => s.Value.Length == 0 ? null : s.Value.ToString())
                                          .LastOrDefault();

            var preferredFileName = $"request-{exchangeInfo.Id}.data";

            // Try to deduce filename from URL 

            if (Uri.TryCreate(exchangeInfo.FullUrl, UriKind.Absolute, out var uri) &&
                !string.IsNullOrWhiteSpace(uri.LocalPath))
                preferredFileName = uri.LocalPath;

            return new RequestBodyAnalysisResult(ResultTitle, context.RequestBodyLength, preferredFileName,
                contentType);
        }
    }

    public class RequestBodyAnalysisResult : FormattingResult
    {
        public RequestBodyAnalysisResult(
            string title,
            long bodyLength, string preferredFileName, string? contentType)
            : base(title)
        {
            BodyLength = bodyLength;
            PreferredFileName = preferredFileName;
            ContentType = contentType;
        }

        public long BodyLength { get; }

        public string PreferredFileName { get; }

        public string? ContentType { get; }
    }
}
