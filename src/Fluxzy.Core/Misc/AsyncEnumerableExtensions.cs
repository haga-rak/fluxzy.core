// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(
            this IAsyncEnumerable<T> items,
            CancellationToken cancellationToken = default)
        {
            var results = new List<T>();

            await foreach (var item in items.WithCancellation(cancellationToken)
                                            .ConfigureAwait(false)) {
                results.Add(item);
            }

            return results;
        }
    }
}
