using System.IO;
using System.Threading.Tasks;

namespace Echoes.H2.Cli
{
    public class H2Connection
    {
        private H2Connection(Stream stream)
        {

        }

        public static async Task<H2Connection> Build(Stream stream, H2StreamSetting initialSetting)
        {
            // Negociating streams 


            return null; 
        }
    }

    public class H2Stream
    {

    }

    public class H2StreamSetting
    {
        public byte Weight { get; set; }

        public int MaxConcurrrentStreams { get; set; } = 100; 
    }

}