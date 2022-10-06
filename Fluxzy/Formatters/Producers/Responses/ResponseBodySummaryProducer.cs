// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Extensions;
using Fluxzy.Screeners;

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

            return new ResponseBodySummaryResult(ResultTitle, context.ResponseBodyLength.Value,
                context.CompressionInfo.CompressionName!, exchangeInfo.GetResponseHeaderValue("content-type"),
                context.ResponseBodyText); 
        }
    }

    public class ResponseBodySummaryResult : FormattingResult
    {
        public ResponseBodySummaryResult(string title,
            long contentLength, string compression, string? contentType, string? bodyText) : base(title)
        {
            ContentLength = contentLength;
            Compression = compression;
            ContentType = contentType;
            BodyText = bodyText;
        }

        public long ContentLength { get;  }

        public string Compression { get;  }

        public string? ContentType { get; }

        public string? BodyText { get;  }
    }
}