// Copyright © 2022 Haga Rakotoharivelo

using System;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class RequestBodyAnalysis : IFormattingProducer<RequestBodyAnalysisResult>
    {
        public string ResultTitle => "Body details";

        public RequestBodyAnalysisResult? Build(ExchangeInfo exchangeInfo, FormattingProducerContext context)
        {
            var preferredFileName = $"request-{exchangeInfo.Id}.data"; 

            // Try to deduce filename from URL 

            if (Uri.TryCreate(exchangeInfo.FullUrl, UriKind.Absolute, out var uri) && 
                !string.IsNullOrWhiteSpace(uri.LocalPath))
            {
                preferredFileName = uri.LocalPath; 
            }

            return new RequestBodyAnalysisResult(ResultTitle, context.RequestBodyLength, preferredFileName); 
        }
    }

    public class RequestBodyAnalysisResult : FormattingResult
    {
        public RequestBodyAnalysisResult(string title, long bodyLength, string preferredFileName) : base(title)
        {
            BodyLength = bodyLength;
            PreferredFileName = preferredFileName;
        }

        public long BodyLength { get; }

        public string PreferredFileName { get; }
    }

}