// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.Headers;
using Fluxzy.Core;
using Fluxzy.Core.Breakpoints;
using Fluxzy.Formatters.Producers.Requests;
using Fluxzy.Rules.Session;

namespace Fluxzy.Rules.Actions.HighLevelActions
{
    /// <summary>
    /// Applies previously captured session data to requests. Adds stored cookies
    /// and headers to outgoing requests for the matching domain.
    /// </summary>
    [ActionMetadata(
        "Apply captured session data to requests. Adds cookies from session store " +
        "and optionally applies stored headers. Works in conjunction with CaptureSessionAction.")]
    public class ApplySessionAction : Action
    {
        public ApplySessionAction()
        {
            ApplyCookies = true;
            ApplyHeaders = true;
            MergeWithExisting = true;
        }

        /// <summary>
        /// Whether to apply stored cookies to requests.
        /// Default is true.
        /// </summary>
        [ActionDistinctive(Description = "Apply stored cookies to request")]
        public bool ApplyCookies { get; set; }

        /// <summary>
        /// Whether to apply stored headers to requests.
        /// Default is true.
        /// </summary>
        [ActionDistinctive(Description = "Apply stored headers to request")]
        public bool ApplyHeaders { get; set; }

        /// <summary>
        /// Whether to merge with existing cookies/headers or replace them.
        /// When true, session cookies are added to existing cookies.
        /// When false, existing cookies are replaced entirely.
        /// Default is true.
        /// </summary>
        [ActionDistinctive(Description = "Merge with existing cookies instead of replacing")]
        public bool MergeWithExisting { get; set; }

        /// <summary>
        /// Optional: specify a different domain to get session from.
        /// Useful for subdomain scenarios (e.g., apply session from "example.com" to "api.example.com").
        /// Supports variable evaluation.
        /// </summary>
        [ActionDistinctive(Description = "Source domain to get session from (optional)")]
        public string? SourceDomain { get; set; }

        public override FilterScope ActionScope => FilterScope.RequestHeaderReceivedFromClient;

        public override string DefaultDescription => "Apply session data";

        public override ValueTask InternalAlter(
            ExchangeContext context, Exchange? exchange, Connection? connection,
            FilterScope scope, BreakPointManager breakPointManager)
        {
            if (exchange == null)
                return default;

            var targetDomain = SourceDomain?.EvaluateVariable(context) ?? context.Authority.HostName;
            var sessionStore = context.VariableContext.SessionStore;

            // Get sessions for this domain and parent domains (e.g., api.github.com AND github.com)
            var matchingSessions = sessionStore.GetSessionsForDomainWithParents(targetDomain).ToList();

            if (!matchingSessions.Any())
                return default;

            // Apply cookies from all matching sessions (merge from parent domains)
            if (ApplyCookies)
            {
                // Collect all cookies from matching sessions
                // Parent domain cookies are added first, then overwritten by more specific domain cookies
                var allSessionCookies = new Dictionary<string, SessionCookie>(StringComparer.OrdinalIgnoreCase);

                // Process in reverse order so more specific domains override parent domains
                foreach (var sessionData in matchingSessions.AsEnumerable().Reverse())
                {
                    sessionData.RemoveExpiredCookies();
                    foreach (var cookie in sessionData.Cookies.Values.Where(c => !c.IsExpired()))
                    {
                        allSessionCookies[cookie.Name] = cookie;
                    }
                }

                if (!allSessionCookies.Any())
                    return default;

                string cookieValue;

                if (MergeWithExisting)
                {
                    // Get existing cookies
                    var existingCookieHeaders = exchange.GetRequestHeaders()
                        .Where(h => h.Name.Span.Equals("Cookie", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (existingCookieHeaders.Any())
                    {
                        // Parse existing cookies
                        var existingCookies = HttpHelper.ReadRequestCookies(
                            existingCookieHeaders.Select(h => new GenericHeaderField(
                                h.Name.Span.ToString(), h.Value.Span.ToString())));

                        // Build merged cookie set (session cookies override existing with same name)
                        var mergedCookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                        // Add existing cookies first
                        foreach (var cookie in existingCookies)
                        {
                            mergedCookies[cookie.Name] = cookie.Value;
                        }

                        // Override/add session cookies
                        foreach (var sessionCookie in allSessionCookies.Values)
                        {
                            mergedCookies[sessionCookie.Name] = sessionCookie.Value;
                        }

                        cookieValue = string.Join("; ", mergedCookies.Select(kvp =>
                            $"{HttpUtility.UrlEncode(kvp.Key)}={HttpUtility.UrlEncode(kvp.Value)}"));
                    }
                    else
                    {
                        // No existing cookies, just use session cookies
                        cookieValue = string.Join("; ", allSessionCookies.Values.Select(c =>
                            $"{HttpUtility.UrlEncode(c.Name)}={HttpUtility.UrlEncode(c.Value)}"));
                    }

                    // Remove existing and add merged
                    context.RequestHeaderAlterations.Add(new HeaderAlterationDelete("Cookie"));
                    context.RequestHeaderAlterations.Add(new HeaderAlterationAdd("Cookie", cookieValue));
                }
                else
                {
                    // Replace entirely with session cookies
                    cookieValue = string.Join("; ", allSessionCookies.Values.Select(c =>
                        $"{HttpUtility.UrlEncode(c.Name)}={HttpUtility.UrlEncode(c.Value)}"));
                    context.RequestHeaderAlterations.Add(new HeaderAlterationDelete("Cookie"));
                    context.RequestHeaderAlterations.Add(new HeaderAlterationAdd("Cookie", cookieValue));
                }
            }

            // Apply headers (only from exact domain match for headers)
            var exactSessionData = matchingSessions.FirstOrDefault();
            if (ApplyHeaders && exactSessionData?.Headers.Any() == true)
            {
                foreach (var (headerName, headerValue) in exactSessionData.Headers)
                {
                    if (MergeWithExisting)
                    {
                        // Only add if not already present
                        var existing = exchange.GetRequestHeaders()
                            .Any(h => h.Name.Span.Equals(headerName, StringComparison.OrdinalIgnoreCase));

                        if (!existing)
                        {
                            context.RequestHeaderAlterations.Add(
                                new HeaderAlterationAdd(headerName, headerValue));
                        }
                    }
                    else
                    {
                        // Replace the header
                        context.RequestHeaderAlterations.Add(new HeaderAlterationDelete(headerName));
                        context.RequestHeaderAlterations.Add(
                            new HeaderAlterationAdd(headerName, headerValue));
                    }
                }
            }

            return default;
        }

        public override IEnumerable<ActionExample> GetExamples()
        {
            yield return new ActionExample(
                "Apply session cookies to requests",
                new ApplySessionAction { ApplyCookies = true, ApplyHeaders = false });

            yield return new ActionExample(
                "Apply all session data (cookies and headers)",
                new ApplySessionAction { ApplyCookies = true, ApplyHeaders = true });

            yield return new ActionExample(
                "Apply session from a specific domain",
                new ApplySessionAction
                {
                    ApplyCookies = true,
                    SourceDomain = "auth.example.com"
                });

            yield return new ActionExample(
                "Replace existing cookies entirely with session cookies",
                new ApplySessionAction
                {
                    ApplyCookies = true,
                    MergeWithExisting = false
                });
        }
    }
}
