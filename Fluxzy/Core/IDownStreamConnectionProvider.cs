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
        /// UpdateTags le provider
        /// </summary>
        /// <returns></returns>
        IReadOnlyCollection<IPEndPoint> Init(CancellationToken token); 

        ValueTask<TcpClient?> GetNextPendingConnection();

        IReadOnlyCollection<IPEndPoint> ListenEndpoints { get;  }

    }
}