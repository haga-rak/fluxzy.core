using System;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Core
{
    public interface IDownStreamConnectionProvider : IDisposable
    {
        /// <summary>
        /// Initialize le provider
        /// </summary>
        /// <returns></returns>
        void Init(CancellationToken token); 

        Task<IDownStreamConnection> GetNextPendingConnection();
    }
}