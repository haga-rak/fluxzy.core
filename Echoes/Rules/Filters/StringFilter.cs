// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text.RegularExpressions;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public abstract class StringFilter : Filter
    {
        protected override bool InternalApply(IExchange exchange)
        {
            var inputList = GetMatchInputs(exchange);

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

        protected abstract IEnumerable<string> GetMatchInputs(IExchange exchange);

        public StringSelectorOperation Operation { get; set; } = StringSelectorOperation.Exact;

        public bool CaseSensitive { get; set; }
        
        public string Pattern { get; set; }
        
        public override string FriendlyName => $"{Operation.GetDescription()} : `{Pattern}`";
    }

    public enum SelectorCollectionOperation
    {
        Or,
        And
    }

    public enum StringSelectorOperation
    {
        [Description("equals")]
        Exact,

        [Description("contains")]
        Contains,

        [Description("starts with")]
        StartsWith,

        [Description("ends with")]
        EndsWith,

        [Description("matchs (regex)")]
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

    public static class GenericDescriptionExtension
    {
        public static string GetDescription<T>(this T enumerationValue)
            where T : struct
        {
            var type = typeof(T);

            if (!type.IsEnum)
            {
                throw new ArgumentException($"{nameof(enumerationValue)} must be an enum");
            }
            
            var memberInfo = type.GetMember(enumerationValue.ToString());

            if (memberInfo.Length > 0)
            {
               var attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return enumerationValue.ToString();
        }
    }
}