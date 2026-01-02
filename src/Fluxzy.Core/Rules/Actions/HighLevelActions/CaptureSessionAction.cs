// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Formatters.Producers.Requests;
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
        "other headers like Authorization. Can also capture cookies from request headers for " +
        "intercepting ongoing sessions. Stored data can be replayed using ApplySessionAction.")]
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
        /// Whether to capture cookies from Cookie request headers.
        /// This is useful when the proxy is inserted into an ongoing web session
        /// where cookies are already set in the browser. Default is false.
        /// </summary>
        [ActionDistinctive(Description = "Capture cookies from Cookie request headers (for ongoing sessions)")]
        public bool CaptureRequestCookies { get; set; }

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

            var requestDomain = context.Authority.HostName;
            var sessionStore = context.VariableContext.SessionStore;

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
                            // Determine storage domain: use cookie's Domain attribute if present, otherwise request host
                            var storageDomain = NormalizeCookieDomain(cookie.Domain) ?? requestDomain;
                            var sessionData = sessionStore.GetOrCreateSession(storageDomain);

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

                                sessionData.SetCookie(cookie.Name, cookie.Value, cookie.Path, expires, storageDomain);
                            }
                        }
                    }
                }
            }

            // Capture cookies from Cookie request headers (passive capture for ongoing sessions)
            if (CaptureRequestCookies)
            {
                var requestHeaders = exchange.GetRequestHeaders().ToList();
                var requestCookies = HttpHelper.ReadRequestCookies(
                    requestHeaders.Select(h => (GenericHeaderField)h));

                if (requestCookies.Any())
                {
                    var sessionData = sessionStore.GetOrCreateSession(requestDomain);

                    foreach (var cookie in requestCookies)
                    {
                        // Only add if not already present (Set-Cookie takes precedence)
                        if (!sessionData.Cookies.ContainsKey(cookie.Name))
                        {
                            sessionData.SetCookie(cookie.Name, cookie.Value, path: null, expires: null, domain: requestDomain);
                        }
                    }
                }
            }

            // For header capture, use request domain
            var sessionDataForHeaders = sessionStore.GetOrCreateSession(requestDomain);

            // Capture specified headers from response
            if (CaptureHeaders?.Any() == true)
            {
                var responseHeaders = exchange.GetResponseHeaders()?.ToList();
                if (responseHeaders != null)
                {
                    foreach (var headerName in CaptureHeaders)
                    {
                        var matchingHeader = responseHeaders
                            .FirstOrDefault(h => h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));

                        if (matchingHeader != null)
                        {
                            sessionDataForHeaders.SetHeader(headerName, matchingHeader.Value.Span.ToString());
                        }
                    }
                }

                // Also capture from request headers (useful for Authorization that's sent by client)
                var requestHeaders = exchange.GetRequestHeaders().ToList();
                foreach (var headerName in CaptureHeaders)
                {
                    var matchingHeader = requestHeaders
                        .FirstOrDefault(h => h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));

                    if (matchingHeader != null)
                    {
                        sessionDataForHeaders.SetHeader(headerName, matchingHeader.Value.Span.ToString());
                    }
                }

                sessionDataForHeaders.LastUpdated = DateTime.UtcNow;
            }

            return default;
        }

        /// <summary>
        /// Normalizes a cookie domain by removing leading dot if present.
        /// Returns null if domain is null or empty.
        /// </summary>
        private static string? NormalizeCookieDomain(string? domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return null;

            // Remove leading dot (e.g., ".github.com" -> "github.com")
            return domain.TrimStart('.');
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

            yield return new ActionExample(
                "Capture cookies from request headers (for ongoing sessions)",
                new CaptureSessionAction
                {
                    CaptureCookies = true,
                    CaptureRequestCookies = true
                });
        }
    }
}
