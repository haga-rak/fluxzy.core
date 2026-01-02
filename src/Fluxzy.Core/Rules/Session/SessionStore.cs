// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Fluxzy.Rules.Session
{
    /// <summary>
    /// Thread-safe storage for session data per domain.
    /// Tied to the proxy lifetime via VariableContext.
    /// </summary>
    public class SessionStore
    {
        private readonly ConcurrentDictionary<string, SessionData> _sessions;

        public SessionStore()
        {
            _sessions = new ConcurrentDictionary<string, SessionData>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Get session data for a domain, returns null if not found
        /// </summary>
        public SessionData? GetSession(string domain)
        {
            if (string.IsNullOrEmpty(domain))
                return null;

            return _sessions.TryGetValue(domain, out var session) ? session : null;
        }

        /// <summary>
        /// Get existing session or create a new one for the domain
        /// </summary>
        public SessionData GetOrCreateSession(string domain)
        {
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentNullException(nameof(domain));

            return _sessions.GetOrAdd(domain, d => new SessionData(d));
        }

        /// <summary>
        /// Store or update session data for a domain
        /// </summary>
        public void SetSession(string domain, SessionData sessionData)
        {
            if (string.IsNullOrEmpty(domain))
                throw new ArgumentNullException(nameof(domain));

            _sessions.AddOrUpdate(domain, sessionData, (_, _) => sessionData);
        }

        /// <summary>
        /// Clear session data for a specific domain
        /// </summary>
        public void ClearSession(string domain)
        {
            if (!string.IsNullOrEmpty(domain))
            {
                _sessions.TryRemove(domain, out _);
            }
        }

        /// <summary>
        /// Clear all session data
        /// </summary>
        public void ClearAll()
        {
            _sessions.Clear();
        }

        /// <summary>
        /// Get the number of stored sessions
        /// </summary>
        public int Count => _sessions.Count;

        /// <summary>
        /// Get all sessions that match the domain or its parent domains.
        /// For example, for "api.github.com", returns sessions for "api.github.com" and "github.com".
        /// Sessions are returned in order from most specific (exact match) to least specific (parent domains).
        /// </summary>
        public IEnumerable<SessionData> GetSessionsForDomainWithParents(string domain)
        {
            if (string.IsNullOrEmpty(domain))
                yield break;

            // First try exact match
            if (_sessions.TryGetValue(domain, out var exactSession))
            {
                yield return exactSession;
            }

            // Then check parent domains
            var parts = domain.Split('.');

            // Skip if already at TLD level (e.g., "com") or no subdomain
            if (parts.Length <= 2)
                yield break;

            // Try each parent domain (e.g., for "api.github.com", try "github.com")
            for (var i = 1; i < parts.Length - 1; i++)
            {
                var parentDomain = string.Join(".", parts, i, parts.Length - i);

                if (_sessions.TryGetValue(parentDomain, out var parentSession))
                {
                    yield return parentSession;
                }
            }
        }
    }
}
