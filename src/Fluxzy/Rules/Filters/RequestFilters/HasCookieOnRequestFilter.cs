// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Formatters.Producers.Requests;

namespace Fluxzy.Rules.Filters.RequestFilters
{
    [FilterMetaData(
        LongDescription = "Exchange having a request cookie with a specific name"
    )]
    public class HasCookieOnRequestFilter : StringFilter
    {
        public HasCookieOnRequestFilter(string cookieName, string pattern)
            : base(pattern)
        {
            Name = cookieName;
        }

        public HasCookieOnRequestFilter(string cookieName, string pattern, StringSelectorOperation operation)
            : base(pattern, operation)
        {
            Name = cookieName;
        }

        [FilterDistinctive]
        public string Name { get; } 

        public override string GenericName => "Cookie";

        public override string ShortName => "ckie";

        public override bool PreMadeFilter => false;

        public override FilterScope FilterScope => FilterScope.RequestHeaderReceivedFromClient;
        
        protected override IEnumerable<string> GetMatchInputs(IAuthority authority, IExchange? exchange)
        {
            if (exchange == null)
                yield break;

            var requestCookies = HttpHelper.ReadRequestCookies(exchange.GetRequestHeaders().Select(h => (GenericHeaderField) h));

            foreach (var cookie in requestCookies) {
                if (cookie.Name.Equals(Name, StringComparison.OrdinalIgnoreCase))
                    yield return cookie.Value; 
            }
        }
    }


}
