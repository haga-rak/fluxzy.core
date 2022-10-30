// Copyright © 2022 Haga RAKOTOHARIVELO

using System.Collections.Generic;

namespace Fluxzy.Misc
{
    public static class CollectionExtensions
    {
        public static void AddRange<T>(this HashSet<T> collection, IEnumerable<T> items)
        {
            foreach (var item in items)
                collection.Add(item);
        }
    }
}
