using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public interface IH2StreamWriter
    {
        ValueTask<H2FrameReadResult> WriteFrame(Stream stream, IBodyFrame frame , CancellationToken cancellationToken); 
    }
}