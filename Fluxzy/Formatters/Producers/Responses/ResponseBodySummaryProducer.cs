// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using Fluxzy.Extensions;

namespace Fluxzy.Formatters.Producers.Responses
{
    public class ResponseBodySummaryProducer : IFormattingProducer<ResponseBodySummaryResult>
    {
        public string ResultTitle => "Body summary";

        public ResponseBodySummaryResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            if (exchangeInfo.ResponseHeader?.Headers == null
                || context.ResponseBodyLength == null
                || context.CompressionInfo == null)
                return null;

            var preferredFileName = $"response-{exchangeInfo.Id}.data";

            // Try to deduce filename from URL 

            if (Uri.TryCreate(exchangeInfo.FullUrl, UriKind.Absolute, out var uri) &&
                !string.IsNullOrWhiteSpace(uri.LocalPath))
                preferredFileName = uri.LocalPath;

            return new ResponseBodySummaryResult(ResultTitle, context.ResponseBodyLength.Value,
                context.CompressionInfo.CompressionName!, exchangeInfo.GetResponseHeaderValue("content-type"),
                context.ResponseBodyText, preferredFileName);
        }
    }

    public class ResponseBodySummaryResult : FormattingResult
    {
        public ResponseBodySummaryResult(
            string title,
            long contentLength, string compression, string? contentType, string? bodyText, string preferredFileName)
            : base(title)
        {
            ContentLength = contentLength;
            Compression = compression;
            ContentType = contentType;
            BodyText = bodyText;
            PreferredFileName = preferredFileName;
        }

        public long ContentLength { get; }

        public string Compression { get; }

        public string? ContentType { get; }

        public string? BodyText { get; }

        public string PreferredFileName { get; }
    }
}
