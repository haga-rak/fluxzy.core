using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core.Utils
{
    public class QuickSlim : IDisposable
    {
        private readonly SemaphoreSlim _semaphoreSlim;

        private QuickSlim(SemaphoreSlim semaphoreSlim)
        {
            _semaphoreSlim = semaphoreSlim;
        }

        private async Task Lock()
        {
            await _semaphoreSlim.WaitAsync().ConfigureAwait(false);
        }
        
        public void Dispose()
        {
            _semaphoreSlim.Release();
        }

        public static async Task<IDisposable> Lock(SemaphoreSlim semaphoreSlim)
        {
            var quickSlim = new QuickSlim(semaphoreSlim); ;
            await quickSlim.Lock().ConfigureAwait(false);
            return quickSlim;
        }
    }
}