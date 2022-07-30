// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using Echoes.Clients;

namespace Echoes.Rules.Filters
{
    public class FilterCollection : Filter
    {
        public List<Filter> Children { get; set; }

        public SelectorCollectionOperation Operation { get; set; }

        protected override bool InternalApply(Exchange exchange)
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
}