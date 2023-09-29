// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class AuthorizationBearerProducer : IFormattingProducer<AuthorizationBearerResult>
    {
        public string ResultTitle => " \"Bearer token\"";

        public AuthorizationBearerResult? Build(
            ExchangeInfo exchangeInfo,
            ProducerContext context)
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

            return new AuthorizationBearerResult(
                ResultTitle, token);
        }
    }

    public class AuthorizationBearerResult : FormattingResult
    {
        public AuthorizationBearerResult(string title, string token)
            : base(title)
        {
            Token = token;
        }

        public string Token { get; }
    }
}
