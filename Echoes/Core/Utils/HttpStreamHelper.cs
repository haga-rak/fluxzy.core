using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using ICSharpCode.SharpZipLib.BZip2;

namespace Echoes.Core.Utils
{
    public class HttpStreamHelper
    {
        private static int ReadNextChunckBlockSize(Stream stream)
        {
            byte [] buffer = new byte[1];
            var stringBuilder = new StringBuilder(string.Empty);
            var readen = 0;

            while ((readen = stream.Read(buffer, 0, buffer.Length)) > 0)
            {
                stringBuilder.Append(Encoding.ASCII.GetString(buffer, 0 ,readen));
                var currentValue = stringBuilder.ToString(); 

                if (currentValue.EndsWith("\r\n"))
                {
                    return int.Parse(currentValue.Replace("\r\n", string.Empty), System.Globalization.NumberStyles.HexNumber);
                }
            }

            return -1; 
        }

        private static async Task<bool> ReadExactlyAsync(MemoryStream destinationStream, Stream sourceStream, int length)
        {
            byte [] buffer = new byte[length];
            int readen = 0;
            int remaining = length;

            while ((readen = await sourceStream.ReadAsync(buffer, 0, remaining).ConfigureAwait(false)) > 0)
            {
                destinationStream.Write(buffer, 0, readen);

                remaining -= length;

                if (remaining == 0)
                    return true; 
            }

            // EOF  was reached before chunck size reached
            return false; 
        }

        public static async Task<Stream> ReadChunckedStream(Stream chunckedStream)
        {
            using (var workStream = new BufferedStream(chunckedStream, 8192))
            {
                var result = new MemoryStream();
                var currentSize = 0;

                while ((currentSize = ReadNextChunckBlockSize(workStream)) > 0)
                {
                    var success = await ReadExactlyAsync(result, workStream, currentSize).ConfigureAwait(false);

                    if (!success)
                        break; // EOF was reached !!!
                }

                result.Seek(0, SeekOrigin.Begin); // Rewing the stream for reader

                return result;
            }
        }

        public static Stream RemoveCompressionStream(Stream original, HttpCompressionMode compressionMode)
        {
            switch (compressionMode)
            {
                case HttpCompressionMode.Gzip:
                    return new GZipStream(original, CompressionMode.Decompress, false);
                case HttpCompressionMode.Deflate:
                    return new DeflateStream(original, CompressionMode.Decompress, false);
                //case HttpCompressionMode.Brotli:
                //    return new BrotliStream(original, CompressionMode.Decompress, false);
                case HttpCompressionMode.Bzip2:
                    return new BZip2InputStream(original);
                default:
                    return original;
            }
        }
    }

}