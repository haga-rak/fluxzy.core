using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.Misc
{
    /// <summary>
    /// Read stream and duplicate read bytes to listener streams
    /// </summary>
    public class DispatchStream : Stream
    {
        private readonly Stream _baseStream;
        private readonly bool _closeOnDone;
        private List<Stream> _destinations;
        private bool _started = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="baseStream">Read stream</param>
        /// <param name="closeOnDone">When readStream reach EOF, close listener streams</param>
        /// <param name="listenerStreams">List of listenerStreams</param>
        public DispatchStream(
            Stream baseStream, bool closeOnDone,
            params Stream[] listenerStreams)
        {
            _baseStream = baseStream;
            _closeOnDone = closeOnDone;
            _destinations = listenerStreams.ToList();
        }

        public override void Flush()
        {
            foreach (var destination in _destinations)
            {
                destination.Flush();
            }
        }

        public void AddListenerStream(Stream stream)
        {
            if (_started)
                throw new InvalidOperationException("Stream reading already started.");

            _destinations.Add(stream);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _started = true;

            var read = _baseStream.Read(buffer, offset, count);

            if (read == 0 && _closeOnDone)
            {
                foreach (var dest in _destinations)
                {
                    dest.Dispose();
                }

                _destinations = null; 
            }
            else
            {
                foreach (var destination in _destinations)
                {
                    destination.Write(buffer, offset, read);
                }
            }

            return read;
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            _started = true;

            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer,
            CancellationToken cancellationToken = new())
        {
            _started = true;

            var read = await _baseStream.ReadAsync(buffer, cancellationToken);

            if (read == 0 && _closeOnDone)
            {
                await Task.WhenAll(
                    _destinations.Select(t => t.DisposeAsync().AsTask()));

                _destinations = null;
            }
            else
            {

                await Task.WhenAll(
                    _destinations.Select(t => t.WriteAsync(buffer.Slice(0, read), cancellationToken).AsTask()));
            }

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead => _baseStream.CanRead;
        public override bool CanSeek => _baseStream.CanSeek;
        public override bool CanWrite => _baseStream.CanWrite;
        public override long Length => _baseStream.Length;

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask DisposeAsync()
        {
             // Console.WriteLine($"Dispatched stream realeased async {_closeOnDone}");

            if (_destinations != null && _closeOnDone)
            {
                foreach (var dest in _destinations)
                {
                    await dest.DisposeAsync();
                }

                _destinations = null; 
            }

            await base.DisposeAsync();
        }

        protected override void Dispose(bool disposing)
        {
            // Console.WriteLine($"Dispatched stream realeased sync {_closeOnDone}");

            if (_destinations != null && _closeOnDone)
            {
                foreach (var dest in _destinations)
                {
                    dest.Dispose();
                }

                _destinations = null;
            }

            base.Dispose(disposing);
        }
    }
}