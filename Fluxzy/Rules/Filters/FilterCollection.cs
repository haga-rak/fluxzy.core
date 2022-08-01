// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Linq;

namespace Fluxzy.Rules.Filters
{
    public class FilterCollection : Filter
    {
        public FilterCollection(params Filter [] filters)
        {
            Children = filters?.ToList() ?? new();
        }

        public List<Filter> Children { get; set; }

        public SelectorCollectionOperation Operation { get; set; }

        protected override bool InternalApply(IAuthority authority, IExchange exchange)
        {
            foreach (var child in Children)
            {
                var res = child.Apply(authority, exchange);

                if (Operation == SelectorCollectionOperation.And && !res)
                    return false; 

                if (Operation == SelectorCollectionOperation.Or && res)
                    return true; 
            }

            return Operation == SelectorCollectionOperation.And; 
        }

        public override FilterScope FilterScope => Children.Max(c => c.FilterScope); 
    }
}