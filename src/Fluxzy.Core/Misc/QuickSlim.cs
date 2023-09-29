// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    public class QuickSlim : IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        private QuickSlim(SemaphoreSlim semaphoreSlim)
        {
            _semaphoreSlim = semaphoreSlim;
        }

        public void Dispose()
        {
            _semaphoreSlim.Release();
        }

        private async Task Lock()
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        }

        public static async Task<IDisposable> Lock(SemaphoreSlim semaphoreSlim)
        {
            var quickSlim = new QuickSlim(semaphoreSlim);
            ;
            await quickSlim.Lock().ConfigureAwait(false);

            return quickSlim;
        }
    }
}
