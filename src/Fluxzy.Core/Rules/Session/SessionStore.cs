// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Concurrent;

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
    }
}
