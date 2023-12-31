// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;

namespace Fluxzy.Misc
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this HashSet<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items) {
                collection.Add(item);
            }
        }
    }
}
