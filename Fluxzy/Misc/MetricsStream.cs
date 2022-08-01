// Copyright © 2022 Haga Rakotoharivelo

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc
{
    /// <summary>
    /// Provides a stream that expose read events
    /// </summary>
    public class MetricsStream : Stream
    {
        private readonly Stream _innerStream;
        private readonly Action _firstBytesReaden;
        private readonly Action<long> _endRead;
        private readonly Action<Exception> _onReadError;
        private readonly CancellationToken _parentToken;

        public long TotalRead { get; private set; }

        public MetricsStream(Stream innerStream, 
            Action firstBytesReaden, 
            Action<long> endRead, 
            Action<Exception> onReadError, 
            CancellationToken parentToken)
        {
            _innerStream = innerStream;
            _firstBytesReaden = firstBytesReaden;
            _endRead = endRead;
            _onReadError = onReadError;
            _parentToken = parentToken;
        }

        public override void Flush()
        {
            _innerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = _innerStream.Read(buffer, offset, count);

            if (TotalRead == 0 && _firstBytesReaden != null)
                _firstBytesReaden();

            if (read == 0)
                _endRead(TotalRead);

            TotalRead += read;

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _innerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException($"{nameof(MetricsStream)} is readonly.");
        }

        public override bool CanRead => _innerStream.CanRead;
        public override bool CanSeek => _innerStream.CanSeek;
        public override bool CanWrite => _innerStream.CanWrite;
        public override long Length => _innerStream.Length;

        public override long Position
        {
            get => _innerStream.Position;
            set => _innerStream.Position = value;
        }

        public Stream InnerStream => _innerStream;

        public override int Read(Span<byte> buffer)
        {
            try
            {
                var res = _innerStream.Read(buffer);
                TotalRead += res;

                return res;
            }
            catch (Exception ex)
            {
                if (_onReadError != null)
                    _onReadError(ex);

                throw;

            }

        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken)
                .ConfigureAwait(false);
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new CancellationToken())
        {
            try
            {
                using var combinedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(_parentToken, cancellationToken); 

                var read = await _innerStream.ReadAsync(buffer, combinedTokenSource.Token)
                    .ConfigureAwait(false);

                if (TotalRead == 0 && _firstBytesReaden != null)
                    _firstBytesReaden();

                if (read == 0)
                    _endRead(TotalRead);

                TotalRead += read;

                return read;
            }
            catch (Exception ex)
            {
                if (_onReadError != null)
                    _onReadError(ex);

                throw; 

            }
        }
    }
}