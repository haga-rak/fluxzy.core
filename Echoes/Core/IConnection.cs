using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Echoes.Core
{
    /// <summary>
    /// Connection between a remote 
    /// </summary>
    public interface IConnection : IDisposable
    {
        /// <summary>
        /// Message identifier
        /// </summary>
        Guid Id { get;  }

        int IncId { get; }

        Stream WriteStream { get;  }

        Stream ReadStream { get;  }

        //   Stream Stream { get;  }

        int LocalPort { get; }

        int RemotePort { get; }

        IPAddress LocalAddress { get; }

        IPAddress RemoteAddress { get; }

        /// <summary>
        /// Connection release. 
        /// </summary>
        /// <param name="closeConnection">Instructs that this connexion should be released</param>
        Task Release(bool closeConnection);

        DateTime InstantConnecting { get;  }

        DateTime InstantConnected { get;  }

        IHttpStreamReader GetHttpStreamReader();

        bool IsWebSocketConnection { get; set; }
    }
}