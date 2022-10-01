// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Linq;
using Fluxzy.Readers;
using Fluxzy.Screeners;

namespace Fluxzy.Formatters.Producers
{
    public class AuthorizationBearerProducer : IFormattingProducer<AuthorizationBearerFormattingResult>
    {
        public string ResultTitle => " \"Bearer token\"";

        public AuthorizationBearerFormattingResult? Build(ExchangeInfo exchangeInfo,
            FormattingProducerParam producerSetting, IArchiveReader archiveReader)
        {
            var headers = exchangeInfo.GetRequestHeaders()?.ToList();

            if (headers == null)
                return null;

            var targetHeader =
                headers.FirstOrDefault(h =>
                    h.Name.Span.Equals("Authorization", StringComparison.OrdinalIgnoreCase)
                    && h.Value.Span.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase));

            if (targetHeader == null)
                return null;

            var token = targetHeader.Value.Span.Slice("Bearer ".Length).ToString();

            return new AuthorizationBearerFormattingResult(
               ResultTitle, token);
        }
    }

    public class AuthorizationBearerFormattingResult : FormattingResult
    {
        public string Token { get; }

        public AuthorizationBearerFormattingResult(string title, string token)
            : base(title)
        {
            Token = token;
        }
    }
}