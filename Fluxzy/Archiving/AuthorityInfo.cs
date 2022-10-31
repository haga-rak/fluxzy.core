// Copyright © 2022 Haga Rakotoharivelo

using System.Text.Json.Serialization;
using Fluxzy.Clients;

namespace Fluxzy
{
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

        public string HostName { get; set; }

        public int Port { get; set; }

        public bool Secure { get; set; }
    }
}