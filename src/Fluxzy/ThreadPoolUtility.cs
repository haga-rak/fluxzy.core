// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Threading;

namespace Fluxzy
{
    internal static class ThreadPoolUtility
    {
        public static void AutoAdjustThreadPoolSize(int concurrentCount)
        {
            var minCount = concurrentCount + 4;

            if (minCount > 64) {
                minCount = 64; 
            }

            var maxCount = minCount * 2;

            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minCompletionPortThreads); 

            if (minWorkerThreads < minCount || minCompletionPortThreads < minCount) {
                ThreadPool.SetMinThreads(minCount, minCount);
            }

            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxCompletionPortThreads);

            if (maxWorkerThreads < maxCount || maxCompletionPortThreads < maxCount) {
                ThreadPool.SetMaxThreads(maxCount, maxCount);
            }
        }

    }
}
