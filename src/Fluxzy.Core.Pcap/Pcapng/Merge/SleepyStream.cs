// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

namespace Fluxzy.Core.Pcap.Pcapng.Merge
{
    /// <summary>
    ///  Hey! This is a stream-like object but accepting read-only operation.
    ///  It saves the last offset read and allows closing the file descriptor
    ///  in between reads.
    /// </summary>
    internal class SleepyStream : IDisposable
    {
        private readonly Func<Stream> _streamFactory;
        private long _offset;
        private Stream? _pendingStream;
        private bool _eof;

        public SleepyStream(Func<Stream> streamFactory)
        {
            _streamFactory = streamFactory;
        }

        public void Sleep()
        {
            if (_pendingStream != null)
            {
                _pendingStream.Dispose();
                _pendingStream = null;
            }
        }

        private int Read(Span<byte> buffer)
        {
            if (_eof)
                return 0;

            var stream = GetStream();

            var read = stream.Read(buffer);
            _offset += read;

            if (read == 0)
            {
                _eof = true;
            }

            return read;
        }

        /// <summary>
        ///  Read exactly the byte count in the provided buffer. Returns false otherwise. 
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public bool ReadExact(Span<byte> buffer)
        {
            var totalRead = 0;

            var slidingBuffer = buffer;

            while (totalRead < buffer.Length)
            {
                var read = Read(slidingBuffer.Slice(totalRead));

                if (read == 0)
                {
                    return false;
                }

                totalRead += read;
            }

            return true;
        }

        private Stream GetStream()
        {
            if (_pendingStream != null)
            {
                return _pendingStream;
            }

            _pendingStream = _streamFactory();
            _pendingStream.Seek(_offset, SeekOrigin.Begin);

            return _pendingStream;
        }

        public void Dispose()
        {
            _pendingStream?.Dispose();
        }
    }
}
