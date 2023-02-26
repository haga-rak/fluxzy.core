using System.Net;

namespace Fluxzy.Interop.Pcap;

public interface ICaptureContext : IAsyncDisposable
{
    void Start(); 
    
    void Include(IPAddress remoteAddress, int remotePort);

    long Subscribe(string outFileName,
        IPAddress remoteAddress, int remotePort, int localPort);

    void Flush(); 
    
    ValueTask Unsubscribe(long subscription);
}