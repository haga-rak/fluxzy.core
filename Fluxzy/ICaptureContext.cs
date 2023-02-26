using System;
using System.Net;
using System.Threading.Tasks;

namespace Fluxzy
{
    public interface ICaptureContext : IAsyncDisposable
    {
        Task Start(); 
    
        void Include(IPAddress remoteAddress, int remotePort);

        long Subscribe(string outFileName,
            IPAddress remoteAddress, int remotePort, int localPort);

        void Flush(); 
    
        ValueTask Unsubscribe(long subscription);
    }
}