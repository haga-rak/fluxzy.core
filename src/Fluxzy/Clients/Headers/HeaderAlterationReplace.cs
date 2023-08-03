// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;

namespace Fluxzy.Clients.Headers
{
    public class HeaderAlterationReplace : HeaderAlteration
    {
        public HeaderAlterationReplace(string headerName, string headerValue, bool addIfMissing)
        {
            HeaderName = headerName;
            HeaderValue = headerValue;
            AddIfMissing = addIfMissing;
        }

        public string HeaderName { get; }

        public string HeaderValue { get; }

        public bool AddIfMissing { get; }

        public override void Apply(Header header)
        {
            header.AltReplaceHeaders(HeaderName, HeaderValue, AddIfMissing);
        }
    }
}
