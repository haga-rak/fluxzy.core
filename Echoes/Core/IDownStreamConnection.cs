using System.IO;

namespace Echoes.Core
{
    /// <summary>
    /// Connection to browser
    /// </summary>
    public interface IDownStreamConnection : IConnection
    {
        void UpgradeReadStream(Stream stream,string hostName, int port);

        void Upgrade(Stream stream, string hostName, int port);

        string TargetHostName { get;  }

        int TargetPort { get;  }

        bool IsSecure { get; set; }
    }
}
