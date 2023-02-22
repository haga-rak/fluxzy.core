using System.Net;
using System.Text;

namespace Fluxzy.Interop.Pcap.Cli
{
    public class RawCaptureControl : ICaptureContext
    {
        public void Include(IPAddress remoteAddress, int remotePort)
        {
            throw new NotImplementedException();
        }

        public long Subscribe(string outFileName, IPAddress remoteAddress, int remotePort, int localPort)
        {
            throw new NotImplementedException();
        }

        public ValueTask Unsubscribe(long subscription)
        {
            throw new NotImplementedException();
        }
    }
}
