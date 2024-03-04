// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy
{
    public class CookieFlow
    {
        public CookieFlow(string name, string host, List<CookieTrackingEvent> events)
        {
            Name = name;
            Host = host;
            Events = events;
        }

        public string Name { get; }

        public string Host { get; }

        public List<CookieTrackingEvent> Events { get; }
    }
}
