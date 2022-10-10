// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Fluxzy.Readers;

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

            var requestCookies = new List<RequestCookie>();

            foreach (var headerValue in targetHeaders)
            {
                var cookieLines = headerValue.Value.ToString()
                                             .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim());

                foreach (var cookieLine in cookieLines)
                {
                    var cookieNameValueTab = cookieLine.Split('=');

                    if (cookieNameValueTab.Length < 2)
                        continue;

                    var cookieName = HttpUtility.UrlDecode(cookieNameValueTab[0]);
                    var cookieValue = HttpUtility.UrlDecode(string.Join("=", cookieNameValueTab.Skip(1)));

                    requestCookies.Add(new RequestCookie(cookieName, cookieValue));
                }
            }

            return requestCookies.Any() ? new RequestCookieResult(ResultTitle, requestCookies) : null;

        }
    }

    public class RequestCookieResult : FormattingResult
    {
        public RequestCookieResult(string title, List<RequestCookie> cookies) : base(title)
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