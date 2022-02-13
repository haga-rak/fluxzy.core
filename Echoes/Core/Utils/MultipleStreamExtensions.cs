using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Echoes.Core.Utils
{
    public static class MultipleStreamExtensions
    {
        public static async Task WriteAsync(this Stream[] streams, byte[] buffer, int offset, int length)
        {
            await Task.WhenAll(streams.Select(stream => stream.WriteAsync(buffer, offset, length))).ConfigureAwait(false);
            //await Task.WhenAll(streams.Select(stream => stream.FlushAsync())).ConfigureAwait(false);

        }

        public static async Task CopyTo(this Stream stream, params Stream[] streams)
        {

            byte[] buffer = new byte[8192];
            int readen;

            while ((readen = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                await WriteAsync(streams, buffer, 0, readen).ConfigureAwait(false);
            }

            //await Task.WhenAll(streams.Select(s => s.FlushAsync())).ConfigureAwait(false);
        }
    }
}