// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Formatters.Producers.Responses;
using Fluxzy.Rules.Session;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    /// Captures session data (cookies, headers) from HTTP responses and stores them
    /// for later replay. Session data is stored per domain.
    /// </summary>
    [ActionMetadata(
        "Capture session data from responses. Captures Set-Cookie headers and optionally " +
        "other headers like Authorization. Stored data can be replayed using ApplySessionAction.")]
    public class CaptureSessionAction : Action
    {
        public CaptureSessionAction()
        {
            CaptureCookies = true;
            CaptureHeaders = new List<string>();
        }

        /// <summary>
        /// Whether to capture cookies from Set-Cookie response headers.
        /// Default is true.
        /// </summary>
        [ActionDistinctive(Description = "Capture cookies from Set-Cookie response headers")]
        public bool CaptureCookies { get; set; }

        /// <summary>
        /// List of response header names to capture (e.g., "Authorization", "X-Auth-Token").
        /// These headers will be stored and can be replayed on subsequent requests.
        /// </summary>
        [ActionDistinctive(Description = "List of response header names to capture")]
        public List<string> CaptureHeaders { get; set; }

        public override FilterScope ActionScope => FilterScope.ResponseHeaderReceivedFromRemote;

        public override string DefaultDescription => "Capture session data";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection,
            FilterScope scope, BreakPointManager breakPointManager)
        {
            if (exchange == null)
                return default;

            var domain = context.Authority.HostName;
            var sessionStore = context.VariableContext.SessionStore;
            var sessionData = sessionStore.GetOrCreateSession(domain);

            // Capture cookies from Set-Cookie headers
            if (CaptureCookies)
            {
                var responseHeaders = exchange.GetResponseHeaders();
                if (responseHeaders != null)
                {
                    var setCookieHeaders = responseHeaders
                        .Where(h => h.Name.Span.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));

                    foreach (var header in setCookieHeaders)
                    {
                        if (SetCookieItem.TryParse(header.Value.Span.ToString(), out var cookie) && cookie != null)
                        {
                            // Check if cookie is being deleted (expired or max-age=0)
                            var isExpired = cookie.MaxAge.HasValue && cookie.MaxAge.Value <= 0;
                            var isPastExpired = cookie.Expired != default && DateTime.Now > cookie.Expired;

                            if (isExpired || isPastExpired)
                            {
                                // Cookie is being deleted, remove from session
                                sessionData.Cookies.TryRemove(cookie.Name, out _);
                            }
                            else
                            {
                                // Store the cookie
                                DateTime? expires = cookie.Expired != default ? cookie.Expired : null;

                                // If MaxAge is set, calculate expiration from now
                                if (cookie.MaxAge.HasValue && cookie.MaxAge.Value > 0)
                                {
                                    expires = DateTime.UtcNow.AddSeconds(cookie.MaxAge.Value);
                                }

                                sessionData.SetCookie(cookie.Name, cookie.Value, cookie.Path, expires);
                            }
                        }
                    }
                }
            }

            // Capture specified headers from response
            if (CaptureHeaders?.Any() == true)
            {
                var responseHeaders = exchange.GetResponseHeaders();
                if (responseHeaders != null)
                {
                    foreach (var headerName in CaptureHeaders)
                    {
                        var matchingHeader = responseHeaders
                            .FirstOrDefault(h => h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));

                        if (matchingHeader != null)
                        {
                            sessionData.SetHeader(headerName, matchingHeader.Value.Span.ToString());
                        }
                    }
                }

                // Also capture from request headers (useful for Authorization that's sent by client)
                var requestHeaders = exchange.GetRequestHeaders();
                foreach (var headerName in CaptureHeaders)
                {
                    var matchingHeader = requestHeaders
                        .FirstOrDefault(h => h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));

                    if (matchingHeader != null)
                    {
                        sessionData.SetHeader(headerName, matchingHeader.Value.Span.ToString());
                    }
                }
            }

            sessionData.LastUpdated = DateTime.UtcNow;

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Capture cookies from responses",
                new CaptureSessionAction { CaptureCookies = true });

            yield return new ActionExample(
                "Capture cookies and Authorization header",
                new CaptureSessionAction
                {
                    CaptureCookies = true,
                    CaptureHeaders = new List<string> { "Authorization" }
                });

            yield return new ActionExample(
                "Capture cookies and multiple custom headers",
                new CaptureSessionAction
                {
                    CaptureCookies = true,
                    CaptureHeaders = new List<string> { "Authorization", "X-CSRF-Token", "X-Auth-Token" }
                });
        }
    }
}
