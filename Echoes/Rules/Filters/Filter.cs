// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections;
using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public abstract class Filter
    {
        public Guid Identifier { get; set; } = Guid.NewGuid();

        public bool Inverted { get; set; }

        protected abstract bool InternalApply(Exchange exchange);

        public virtual bool Apply(Exchange exchange)
        {
            var internalApplyResult = InternalApply(exchange);

            return !Inverted ? internalApplyResult : !internalApplyResult;
        }
    }

    public class RequestHeaderFilter : StringFilter
    {
        protected override IEnumerable<string> GetMatchInput(Exchange exchange)
        {
            throw new NotImplementedException("Should read request header"); 
        }

        public string HeaderName { get; set; }
    }

    public static class ExchangeExtensions
    {
        public static string GetUrl(this Exchange exchange)
        {
            throw new NotImplementedException(); 
        }
        public static string GetRequestHeader(this Exchange exchange)
        {
            throw new NotImplementedException(); 
        }
    }
}