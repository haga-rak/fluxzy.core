// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using Fluxzy.Core;

namespace Fluxzy.Clients.Headers
{
    public class HeaderAlterationDelete : HeaderAlteration
    {
        public HeaderAlterationDelete(string headerName)
        {
            HeaderName = headerName;
        }

        public string HeaderName { get; }

        public override void Apply(Header header)
        {
            header.AltDeleteHeader(HeaderName);
        }
    }
}
