// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Rules.Session
{
    /// <summary>
    /// Represents a captured cookie with metadata for session storage.
    /// </summary>
    public class SessionCookie
    {
        public SessionCookie(string name, string value)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Value = value ?? string.Empty;
        }

        /// <summary>
        /// Cookie name
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Cookie value
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Cookie path attribute
        /// </summary>
        public string? Path { get; set; }

        /// <summary>
        /// Cookie domain attribute
        /// </summary>
        public string? Domain { get; set; }

        /// <summary>
        /// Cookie expiration date
        /// </summary>
        public DateTime? Expires { get; set; }

        /// <summary>
        /// HttpOnly flag
        /// </summary>
        public bool HttpOnly { get; set; }

        /// <summary>
        /// Secure flag
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// Check if the cookie is expired
        /// </summary>
        public bool IsExpired()
        {
            return Expires.HasValue && DateTime.UtcNow > Expires.Value;
        }
    }
}
