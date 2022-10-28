﻿namespace Fluxzy.Rules.Filters
{
    public class TagContainsFilter : Filter
    {

        public TagContainsFilter(Tag?  tag)
        {
            Tag = tag;
        }

        public Tag? Tag { get; }
        

        protected override bool InternalApply(IAuthority? authority, IExchange? exchange, IFilteringContext? filteringContext)
        {
            if (exchange?.Tags == null || Tag == null)
                return false;

            return exchange.Tags.Contains(Tag); 
        }

        public override FilterScope FilterScope => FilterScope.OutOfScope;

        public override string AutoGeneratedName => $"Tag contains {Tag?.Value}";
    }
}