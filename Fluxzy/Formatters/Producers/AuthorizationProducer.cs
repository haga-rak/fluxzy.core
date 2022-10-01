// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using Fluxzy.Readers;
using Fluxzy.Screeners;
using System.Linq;

namespace Fluxzy.Formatters.Producers
{

    public class AuthorizationProducer : IFormattingProducer<AuthorizationResult>
    {
        public string ResultTitle => "Authorization Header";

        public AuthorizationResult? Build(ExchangeInfo exchangeInfo, IArchiveReader archiveReader)
        {
            var headers = exchangeInfo.GetRequestHeaders()?.ToList();

            if (headers == null)
                return null;

            var targetHeader =
                headers.FirstOrDefault(h =>
                    h.Name.Span.Equals("Authorization", StringComparison.OrdinalIgnoreCase));

            if (targetHeader == null)
                return null;

            var value = targetHeader.Value.Span.Trim().ToString();
            return new AuthorizationResult(ResultTitle, value);
        }
    }

    public class AuthorizationResult : FormattingResult
    {
        public AuthorizationResult(string title, string value) : base(title)
        {
            Value = value;
        }

        public string Value { get;  }
    }
}