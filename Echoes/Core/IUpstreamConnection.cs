using System;

namespace Echoes.Core
{
    /// <summary>
    /// Connection to server
    /// </summary>
    public interface IUpstreamConnection : IConnection, IReleasedInstantInfo
    {
        string Hostname { get; }

        bool Secure { get; }

        // This object will be on a list so it's usefull
        int GetHashCode();

        bool Equals(object obj);

        DateTime InstantDnsSolveStartUtc { get; }
        
        DateTime InstantDnsSolveEndUtc { get; }

        bool ShouldBeClose { get; set; }

    }


    
}