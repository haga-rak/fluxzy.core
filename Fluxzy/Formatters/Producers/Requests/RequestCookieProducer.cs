// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Formatters.Producers.Requests
{
    public class RequestCookieProducer : IFormattingProducer<RequestCookieResult>
    {
        public string ResultTitle => "Cookies";

        public RequestCookieResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            var headers = exchangeInfo.GetRequestHeaders()?.ToList();

            if (headers == null)
                return null;

            var targetHeaders =
                headers.Where(h =>
                    h.Name.Span.Equals("Cookie", StringComparison.OrdinalIgnoreCase));

            var requestCookies = HttpHelper.ReadRequestCookies(targetHeaders);

            return requestCookies.Any() ? new RequestCookieResult(ResultTitle, requestCookies) : null;
        }
    }

    public class RequestCookieResult : FormattingResult
    {
        public RequestCookieResult(string title, List<RequestCookie> cookies)
            : base(title)
        {
            Cookies = cookies;
        }

        public List<RequestCookie> Cookies { get; }
    }

    public class RequestCookie
    {
        public RequestCookie(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}
