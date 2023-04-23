// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Formatters.Producers.Responses;

namespace Fluxzy.Formatters.Producers.Requests
{
    internal static class HttpHelper
    {
        public static List<RequestCookie> ReadRequestCookies(IEnumerable<GenericHeaderField> targetHeaders)
        {
            var requestCookies = new List<RequestCookie>();

            foreach (var headerValue in targetHeaders.Where(h => h.Name.Equals("Cookie", StringComparison.OrdinalIgnoreCase))) {
                var cookieLines = headerValue.Value
                                             .Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries)
                                             .Select(s => s.Trim());

                foreach (var cookieLine in cookieLines) {
                    var cookieNameValueTab = cookieLine.Split('=');

                    if (cookieNameValueTab.Length < 2)
                        continue;

                    var cookieName = HttpUtility.UrlDecode(cookieNameValueTab[0]);
                    var cookieValue = HttpUtility.UrlDecode(string.Join("=", cookieNameValueTab.Skip(1)));

                    requestCookies.Add(new RequestCookie(cookieName, cookieValue));
                }
            }

            return requestCookies;
        }

        public static List<SetCookieItem> ReadResponseCookies(IEnumerable<HeaderFieldInfo> cookieHeaders)
        {
            var cookieItems = cookieHeaders.Select(s => {
                if (SetCookieItem.TryParse(s.Value.ToString(), out var cookie))
                    return cookie;

                return null;
            }).Where(s => s != null).OfType<SetCookieItem>().ToList();

            return cookieItems;
        }

        public static bool TryGetQueryStrings(string url, out List<QueryStringItem> result)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) {
                result = null!;

                return false;
            }

            result = GetQueryStrings(uri);

            return true;
        }

        public static List<QueryStringItem> GetQueryStrings(Uri uri)
        {
            var res = HttpUtility.ParseQueryString(uri.Query);

            var items = res.AllKeys.SelectMany(k => res.GetValues(k)?.Select(v => new QueryStringItem(k, v)))
                           .Where(t => t != null)
                           .ToList();

            return items;
        }
    }
}
