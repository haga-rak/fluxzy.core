// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public abstract class StringFilter : Filter
    {
        protected override bool InternalApply(IExchange exchange)
        {
            var inputList = GetMatchInput(exchange);

            var comparisonType = CaseSensitive ? StringComparison.InvariantCulture :
                StringComparison.InvariantCultureIgnoreCase;

            foreach (var input in inputList)
            {
                switch (Operation)
                {
                    case StringSelectorOperation.Exact:
                        if (Pattern.Equals(input, comparisonType))
                            return true;
                        continue; 
                    case StringSelectorOperation.Contains:
                        if (input.Contains(Pattern, comparisonType))
                            return true;
                        continue;
                    case StringSelectorOperation.StartsWith:
                        if (input.StartsWith(Pattern, comparisonType))
                            return true;
                        continue;
                    case StringSelectorOperation.EndsWith:
                        if (input.EndsWith(Pattern, comparisonType))
                            return true;
                        continue;
                    case StringSelectorOperation.Regex:
                        if (Regex.Match(input, Pattern, CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase).Success)
                            return true;
                        continue;
                    default:
                        throw new InvalidOperationException($"Unimplemented string operation {Operation}");
                }
            }

            return false; 
        }

        protected abstract IEnumerable<string> GetMatchInput(IExchange exchange);

        public StringSelectorOperation Operation { get; set; } = StringSelectorOperation.Exact;

        public bool CaseSensitive { get; set; }
        
        public string Pattern { get; set; }
    }

    public enum SelectorCollectionOperation
    {
        Or,
        And
    }

    public enum StringSelectorOperation
    {
        Exact,
        Contains,
        StartsWith,
        EndsWith,
        Regex,
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
}