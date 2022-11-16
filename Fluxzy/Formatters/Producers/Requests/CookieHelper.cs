using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Fluxzy.Formatters.Producers.Responses;

namespace Fluxzy.Formatters.Producers.Requests
{
    internal static class CookieHelper
    {
        public static List<RequestCookie> ReadRequestCookies(IEnumerable<HeaderFieldInfo> targetHeaders)
        {
            var requestCookies = new List<RequestCookie>();
            
            foreach (var headerValue in targetHeaders) {
                var cookieLines = headerValue.Value.ToString()
                                             .Split(new[] {";"}, StringSplitOptions.RemoveEmptyEntries)
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
            var cookieItems = cookieHeaders.Select(s =>
            {
                if (SetCookieItem.TryParse(s.Value.ToString(), out var cookie))
                    return cookie;

                return null;
            }).Where(s => s != null).OfType<SetCookieItem>().ToList();
            return cookieItems;
        }
    }
}