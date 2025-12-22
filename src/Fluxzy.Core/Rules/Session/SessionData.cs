// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Web;

namespace Fluxzy.Rules.Session
{
    /// <summary>
    /// Holds session data (cookies and headers) for a single domain.
    /// Thread-safe for concurrent access.
    /// </summary>
    public class SessionData
    {
        public SessionData(string domain)
        {
            Domain = domain ?? throw new ArgumentNullException(nameof(domain));
            Cookies = new ConcurrentDictionary<string, SessionCookie>(StringComparer.OrdinalIgnoreCase);
            Headers = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Domain this session belongs to
        /// </summary>
        public string Domain { get; }

        /// <summary>
        /// Cookies stored for this session (name -> SessionCookie)
        /// </summary>
        public ConcurrentDictionary<string, SessionCookie> Cookies { get; }

        /// <summary>
        /// Custom headers stored for this session (header name -> value)
        /// </summary>
        public ConcurrentDictionary<string, string> Headers { get; }

        /// <summary>
        /// Timestamp of last update
        /// </summary>
        public DateTime LastUpdated { get; set; }

        /// <summary>
        /// Add or update a cookie
        /// </summary>
        public void SetCookie(string name, string value, string? path = null, DateTime? expires = null)
        {
            var cookie = new SessionCookie(name, value)
            {
                Path = path,
                Expires = expires
            };

            Cookies.AddOrUpdate(name, cookie, (_, _) => cookie);
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Add or update a header
        /// </summary>
        public void SetHeader(string name, string value)
        {
            Headers.AddOrUpdate(name, value, (_, _) => value);
            LastUpdated = DateTime.UtcNow;
        }

        /// <summary>
        /// Remove expired cookies from the session
        /// </summary>
        public void RemoveExpiredCookies()
        {
            var expiredKeys = Cookies
                .Where(kvp => kvp.Value.IsExpired())
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                Cookies.TryRemove(key, out _);
            }
        }

        /// <summary>
        /// Get cookie string for Cookie header (name1=value1; name2=value2)
        /// </summary>
        public string GetCookieHeaderValue()
        {
            RemoveExpiredCookies();

            var cookiePairs = Cookies.Values
                .Where(c => !c.IsExpired())
                .Select(c => $"{HttpUtility.UrlEncode(c.Name)}={HttpUtility.UrlEncode(c.Value)}");

            return string.Join("; ", cookiePairs);
        }
    }
}
