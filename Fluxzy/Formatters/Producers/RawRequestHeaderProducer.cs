// Copyright © 2022 Haga Rakotoharivelo

using System.Text;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers
{
    public class RawRequestHeaderProducer : IFormattingProducer<RawRequestHeaderResult>
    {
        public string ResultTitle => "Raw header (H11 style)";

        public RawRequestHeaderResult? Build(ExchangeInfo exchangeInfo, FormattingProducerContext context)
        {
            var requestHeaders = exchangeInfo.GetRequestHeaders();
            var stringBuilder = new StringBuilder();

            foreach (var requestHeader in requestHeaders)
            {
                stringBuilder.Append(requestHeader.Name.Span);
                stringBuilder.Append(": ");
                stringBuilder.Append(requestHeader.Value.Span);
                stringBuilder.AppendLine(); 
            }

            return new RawRequestHeaderResult(ResultTitle, stringBuilder.ToString());
        }
    }

    public class RawRequestHeaderResult : FormattingResult
    {
        public RawRequestHeaderResult(string title, string rawHeader) : base(title)
        {
            RawHeader = rawHeader;
        }

        public string RawHeader { get; }
    }

}