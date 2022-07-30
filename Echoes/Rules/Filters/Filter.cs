// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public enum FilterScope
    {
        RequestHeaderReceivedFromClient = 1,
        RequestBodyReceivedFromClient,
        ResponseHeaderReceivedFromRemote,
        ResponseBodyReceivedFromRemote
    }


    public abstract class Filter
    {
        
        public Guid Identifier { get; set; } = Guid.NewGuid();

        public bool Inverted { get; set; }

        protected abstract bool InternalApply(Exchange exchange);

        public abstract FilterScope FilterScope { get; }
        
        public virtual bool Apply(Exchange exchange)
        {
            var internalApplyResult = InternalApply(exchange);

            return !Inverted ? internalApplyResult : !internalApplyResult;
        }
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