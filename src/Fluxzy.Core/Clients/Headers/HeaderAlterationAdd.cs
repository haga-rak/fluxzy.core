// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;

namespace Fluxzy.Clients.Headers
{
    public class HeaderAlterationAdd : HeaderAlteration
    {
        public HeaderAlterationAdd(string headerName, string headerValue)
        {
            HeaderName = headerName;
            HeaderValue = headerValue;
        }

        public string HeaderName { get; }

        public string HeaderValue { get; }

        public override void Apply(Header header)
        {
            header.AltAddHeader(HeaderName, HeaderValue);
        }
    }
}
