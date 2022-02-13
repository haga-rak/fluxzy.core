using System;
using System.IO;
using System.Threading.Tasks;

namespace Echoes.Core
{
    public interface IHttpStreamReader
    {
        Task<HeaderReadResult> ReadHeaderAsync(bool skipIfNoData);

        Task<BodyReadResult> ReadBodyAsync(long length, params Stream [] outStreams);

        Task<BodyReadResult> ReadBodyUntilEofAsync(params Stream [] outStreams);

        Task<BodyReadResult> ReadBodyChunkedAsync(params Stream [] outStreams);
    }


    public class HeaderReadResult
    {
        public byte [] Buffer { get; set; }

        public DateTime ? FirstByteReceived { get; set; }

        public DateTime ? LastByteReceived { get; set; }
    }

    public class BodyReadResult
    {
        public long Length { get; set; }

        public DateTime ? FirstByteReceived { get; set; }

        public DateTime ? LastByteReceived { get; set; }

        public static BodyReadResult CreateEmptyResult(IReferenceClock referenceClock)
        {
            var instant = referenceClock.Instant();

            return new BodyReadResult()
            {
                Length =  0L,
                FirstByteReceived = instant,
                LastByteReceived = instant
            };
        }

    }

    
}