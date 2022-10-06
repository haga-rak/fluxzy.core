// Copyright © 2022 Haga Rakotoharivelo

using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers.Responses
{
    public class ResponseBodySummaryProducer : IFormattingProducer<ResponseBodySummaryResult>
    {
        public string ResultTitle => throw new System.NotImplementedException();

        public ResponseBodySummaryResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            throw new System.NotImplementedException();
        }
    }

    public class ResponseBodySummaryResult : FormattingResult
    {
        public ResponseBodySummaryResult(string title,
            long contentLength, string compression, string contentType, string content) : base(title)
        {
            ContentLength = contentLength;
            Compression = compression;
            ContentType = contentType;
            Content = content;
        }

        public long ContentLength { get;  }

        public string Compression { get;  }

        public string ContentType { get; }

        public string Content { get;  }
    }
}