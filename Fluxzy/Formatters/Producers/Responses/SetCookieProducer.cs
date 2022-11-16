// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Fluxzy.Formatters.Producers.Requests;

namespace Fluxzy.Formatters.Producers.Responses
{
    public class SetCookieProducer : IFormattingProducer<SetCookieResult>
    {
        public string ResultTitle => "Set cookies";

        public SetCookieResult? Build(ExchangeInfo exchangeInfo, ProducerContext context)
        {
            var cookieHeaders = exchangeInfo.GetResponseHeaders()
                                            ?.Where(h =>
                                                h.Name.Span.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

            if (cookieHeaders == null)
                return null;

            var cookieItems = CookieHelper.ReadResponseCookies(cookieHeaders);

            return !cookieItems.Any() ? null : new SetCookieResult(ResultTitle, cookieItems);
        }
    }

    public class SetCookieResult : FormattingResult
    {
        public List<SetCookieItem> Cookies { get; }

        public SetCookieResult(string title, IEnumerable<SetCookieItem> cookies)
            : base(title)
        {
            Cookies = cookies.ToList();
        }
    }

    public class SetCookieItem
    {
        public string Name { get; }

        public string Value { get; }

        public string? Domain { get; private set; }

        public string? Path { get; private set; }

        public string? SameSite { get; private set; }

        public DateTime Expired { get; private set; }

        public int? MaxAge { get; private set; }

        public bool Secure { get; private set; }

        public bool HttpOnly { get; private set; }

        public SetCookieItem(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public static bool TryParse(string rawLine, out SetCookieItem? result)
        {
            result = null;

            if (string.IsNullOrWhiteSpace(rawLine))
                return false;

            var mainList = rawLine.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => t.Trim()).ToArray();

            if (!mainList.Any())
                return false;

            var nameValueTab = mainList.First().Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);

            if (nameValueTab.Length < 2)
                return false;

            var name = HttpUtility.UrlDecode(nameValueTab[0]);
            var value = HttpUtility.UrlDecode(string.Join("=", nameValueTab.Skip(1)));

            result = new SetCookieItem(name, value);

            mainList = mainList.Skip(1).ToArray();

            // Parse domain 

            if (mainList.TryGet(l => l.StartsWith("Domain=", StringComparison.OrdinalIgnoreCase),
                    out var domain))
                result.Domain = HttpUtility.UrlDecode(GetValueFromLine(domain!));

            if (mainList.TryGet(l => l.StartsWith("Path=", StringComparison.OrdinalIgnoreCase),
                    out var path))
                result.Path = HttpUtility.UrlDecode(GetValueFromLine(path!));

            if (mainList.TryGet(l => l.StartsWith("SameSite=", StringComparison.OrdinalIgnoreCase),
                    out var sameSite))
                result.SameSite = HttpUtility.UrlDecode(GetValueFromLine(sameSite!));

            if (mainList.TryGet(l => l.StartsWith("Max-Age=", StringComparison.OrdinalIgnoreCase),
                    out var maxAgeString) && int.TryParse(GetValueFromLine(maxAgeString!), out var maxAge))
                result.MaxAge = maxAge;

            if (mainList.TryGet(l => l.StartsWith("Expires=", StringComparison.OrdinalIgnoreCase),
                    out var lineDateString) && DateTime.TryParse(GetValueFromLine(lineDateString!), out var date))
                result.Expired = date;

            if (mainList.Any(t => t.Equals("HttpOnly", StringComparison.OrdinalIgnoreCase)))
                result.HttpOnly = true;

            if (mainList.Any(t => t.Equals("Secure", StringComparison.OrdinalIgnoreCase)))
                result.Secure = true;

            return true;
        }

        private static string GetValueFromLine(string line)
        {
            return line.Substring(line.IndexOf('=') + 1).Trim();
        }
    }

    internal static class LinqExtensions
    {
        public static bool TryGet<T>(this IEnumerable<T> list, Func<T, bool> condition, out T? item)
            where T : class
        {
            item = list.FirstOrDefault(condition);

            return item != null;
        }
    }
}
