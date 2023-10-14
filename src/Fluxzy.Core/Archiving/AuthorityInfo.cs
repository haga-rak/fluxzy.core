// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Text.Json.Serialization;
using Fluxzy.Core;

namespace Fluxzy
{
    /// <summary>
    /// Information about a remote authority
    /// </summary>
    public class AuthorityInfo
    {
        public AuthorityInfo(Authority original)
        {
            HostName = original.HostName;
            Port = original.Port;
            Secure = original.Secure;
        }

        [JsonConstructor]
        public AuthorityInfo(string hostName, int port, bool secure)
        {
            HostName = hostName;
            Port = port;
            Secure = secure;
        }

        /// <summary>
        /// The hostname 
        /// </summary>
        public string HostName { get; set; }

        /// <summary>
        /// Port number
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// true if the request is secure (HTTPS)
        /// </summary>
        public bool Secure { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool Equals(AuthorityInfo other)
        {
            return HostName == other.HostName && Port == other.Port && Secure == other.Secure;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != this.GetType())
            {
                return false;
            }

            return Equals((AuthorityInfo)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(HostName, Port, Secure);
        }

    }
}
