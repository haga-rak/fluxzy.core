// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    public static class ValueTaskUtil
    {
        public static async ValueTask<T[]> WhenAll<T>(IList<ValueTask<T>> tasks)
        {
            if (tasks.Count == 0)
                return Array.Empty<T>();
            
            List<Exception>? exceptions = null;

            var results = new T[tasks.Count];
            for (var i = 0; i < tasks.Count; i++)
                try
                {
                    results[i] = await tasks[i].ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    exceptions ??= new(tasks.Count);
                    exceptions.Add(ex);
                }

            return exceptions is null
                ? results
                : throw new AggregateException(exceptions);
        }
    }
}