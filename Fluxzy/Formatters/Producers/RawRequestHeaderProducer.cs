// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Misc;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers
{
    public class RawRequestHeaderProducer : IFormattingProducer<RawRequestHeaderResult>
    {
        public string ResultTitle => "Raw header (H11 style)";

        public RawRequestHeaderResult? Build(ExchangeInfo exchangeInfo, FormattingProducerContext context)
        {
            var requestHeaders = exchangeInfo.GetRequestHeaders().ToList();
            var stringBuilder = new StringBuilder();

            Http11Parser parser = new Http11Parser(32 * 1024);

            var charBuffer = ArrayPool<char>.Shared.Rent(context.Settings.MaxHeaderLength);

            try
            {
                var spanRes = parser.Write(requestHeaders, charBuffer);

                stringBuilder.Append(spanRes); 
            }
            finally
            {
                ArrayPool<char>.Shared.Return(charBuffer);
            }

            if (context.RequestBodyText != null)
            {
                stringBuilder.Append(context.RequestBodyText); 
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