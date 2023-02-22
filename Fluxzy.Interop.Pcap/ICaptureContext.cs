using System.Net;

namespace Fluxzy.Interop.Pcap;

public interface ICaptureContext
{
    void Include(IPAddress remoteAddress, int remotePort);

    long Subscribe(string outFileName,
        IPAddress remoteAddress, int remotePort, int localPort);
    
    ValueTask Unsubscribe(long subscription);
}