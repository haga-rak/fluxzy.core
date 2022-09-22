using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    public interface IDownStreamConnectionProvider : IDisposable
    {
        /// <summary>
        /// Initialize le provider
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<IPEndPoint> Init(CancellationToken token); 

        Task<TcpClient> GetNextPendingConnection();

        IReadOnlyCollection<IPEndPoint> ListenEndpoints { get;  }

    }
}