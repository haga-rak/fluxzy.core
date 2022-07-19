// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Selectors
{
    public interface ISelector
    {
        bool Apply(Exchange exchange);
    }

    [SelectorTarget(SelectorType.Collection)]
    public class SelectorCollection : ISelector
    {
        public List<ISelector> Children { get; set; }

        public SelectorCollectionOperation Operation { get; set; }

        public bool Apply(Exchange exchange)
        {
            foreach (var child in Children)
            {
                var res = child.Apply(exchange);

                if (Operation == SelectorCollectionOperation.And && !res)
                    return false; 

                if (Operation == SelectorCollectionOperation.Or && res)
                    return true; 
            }

            return Operation == SelectorCollectionOperation.And; 
        }
    }

    public enum SelectorCollectionOperation
    {
        Or, 
        And
    }


    public abstract class StringSelector : ISelector
    {
        public virtual bool Apply(Exchange exchange)
        {
            var pattern = GetMatchInput(exchange);

            // apply rule 

            throw new NotImplementedException(); 
        }

        protected abstract string GetMatchInput(Exchange exchange);

        public StringSelectorOperation Operation { get; set; } = StringSelectorOperation.Exact;

        public bool CaseSensitive { get; set; }
        
        public string Pattern { get; set; }
    }

    public enum StringSelectorOperation
    {
        Exact,
        Regex,
        StartsWith,
        EndsWith,
        RootDomain,
    }

    public enum SelectorType
    {
        Collection,
        Hostname,
        FullUrl,
        RequestHeader,
        RequestBody,
        StatusCode
    }

    public class SelectorTargetAttribute : Attribute
    {
        public SelectorTargetAttribute(SelectorType type)
        {
            Type = type;
        }
        public SelectorType Type { get; }
    }

    [SelectorTarget(SelectorType.FullUrl)]
    public class UrlSelector : StringSelector
    {
        protected override string GetMatchInput(Exchange exchange)
        {
            return exchange.GetUrl();
        }
    }

    [SelectorTarget(SelectorType.RequestHeader)]
    public class RequestHeaderSelector : StringSelector
    {
        protected override string GetMatchInput(Exchange exchange)
        {
            throw new NotImplementedException("Should read request header"); 
        }
    }

    [SelectorTarget(SelectorType.StatusCode)]
    public class StatusCodeSelector : ISelector
    {
        public bool Apply(Exchange exchange)
        {
            return exchange.Response?.Header.StatusCode == StatusCode; 
        }

        public int StatusCode { get; set; }
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