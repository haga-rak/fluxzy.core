// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Linq;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public class FilterCollection : Filter
    {
        public List<Filter> Children { get; set; }

        public SelectorCollectionOperation Operation { get; set; }

        protected override bool InternalApply(IExchange exchange)
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

        public override FilterScope FilterScope => Children.Max(c => c.FilterScope); 
    }
}