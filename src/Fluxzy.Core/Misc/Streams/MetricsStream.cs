// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///     Provides a stream that expose read events
    /// </summary>
    public class MetricsStream : Stream
    {
        private readonly Action<long> _endRead;
        private readonly Action _firstBytesReaden;
        private readonly Action<Exception> _onReadError;
        private readonly CancellationToken _parentToken;

        public MetricsStream(
            Stream innerStream,
            Action firstBytesReaden,
            Action<long> endRead,
            Action<Exception> onReadError,
            CancellationToken parentToken)
        {
            InnerStream = innerStream;
            _firstBytesReaden = firstBytesReaden;
            _endRead = endRead;
            _onReadError = onReadError;
            _parentToken = parentToken;
        }

        public long TotalRead { get; private set; }

        public override bool CanRead => InnerStream.CanRead;

        public override bool CanSeek => InnerStream.CanSeek;

        public override bool CanWrite => InnerStream.CanWrite;

        public override long Length => InnerStream.Length;

        public override long Position {
            get => InnerStream.Position;

            set => InnerStream.Position = value;
        }

        public Stream InnerStream { get; }

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = InnerStream.Read(buffer, offset, count);

            if (TotalRead == 0 && _firstBytesReaden != null) {
                _firstBytesReaden();
            }

            if (read == 0) {
                _endRead(TotalRead);
            }

            TotalRead += read;

            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return InnerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            InnerStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException($"{nameof(MetricsStream)} is readonly.");
        }

        public override int Read(Span<byte> buffer)
        {
            try {
                var res = InnerStream.Read(buffer);
                TotalRead += res;

                return res;
            }
            catch (Exception ex) {
                if (_onReadError != null) {
                    _onReadError(ex);
                }

                throw;
            }
        }

        public override async Task<int> ReadAsync(
            byte[] buffer, int offset, int count,
            CancellationToken cancellationToken)
        {
            return await ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken)
                ;
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            try {
                using var combinedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(_parentToken, cancellationToken);

                var read = await InnerStream.ReadAsync(buffer, combinedTokenSource.Token)
                                            ;

                if (TotalRead == 0 && _firstBytesReaden != null) {
                    _firstBytesReaden();
                }

                if (read == 0) {
                    _endRead(TotalRead);
                }

                TotalRead += read;

                return read;
            }
            catch (Exception ex) {
                if (_onReadError != null) {
                    _onReadError(ex);
                }

                throw;
            }
        }
    }
}
