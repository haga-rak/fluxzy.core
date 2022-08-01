using System.IO;
using System.Threading.Tasks;

namespace Fluxzy.Core.Utils
{

    public static class StreamNetStandar2Extensions
    {
        public static Task WriteAsyncNS2(this Stream stream, byte[] buffer)
        {
            return stream.WriteAsync(buffer, 0, buffer.Length); 
        }
    }
}