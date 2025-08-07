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
        private readonly Action<bool, long> _endRead;
        private readonly Action _firstBytesRead;
        private readonly Action<Exception> _onReadError;
        private readonly long? _expectedLength;
        private readonly CancellationToken _parentToken;

        private bool _firstReadNotified;
        private bool _finalReadNotified;

        public MetricsStream(
            Stream innerStream,
            Action firstBytesRead,
            Action<bool, long> endRead,
            Action<Exception> onReadError,
            bool endConnection,
            long ? expectedLength,
            CancellationToken parentToken)
        {
            InnerStream = innerStream;
            EndConnection = endConnection;
            _firstBytesRead = firstBytesRead;
            _endRead = endRead;
            _onReadError = onReadError;
            _expectedLength = expectedLength;
            _parentToken = parentToken;
            
            if (_expectedLength == 0) {
                NotifyFirstRead();
                NotifyFinalRead();
            }
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

        public bool EndConnection { get; }

        public override void Flush()
        {
            InnerStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var read = InnerStream.Read(buffer, offset, count);

            if (TotalRead == 0) {
                NotifyFirstRead();
            }

            TotalRead += read;

            if ((read == 0 && _expectedLength == null) ||
                (_expectedLength != null && _expectedLength >= TotalRead) )
            {
                NotifyFinalRead();
            }

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
                .ConfigureAwait(false);
        }
        
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = new())
        {
            try {
                using var combinedTokenSource =
                    CancellationTokenSource.CreateLinkedTokenSource(_parentToken, cancellationToken);

                var read = await InnerStream.ReadAsync(buffer, combinedTokenSource.Token)
                                            .ConfigureAwait(false);

                if (TotalRead == 0)
                {
                    NotifyFirstRead();
                }

                TotalRead += read;

                if ((read == 0 && _expectedLength == null) || (_expectedLength != null && _expectedLength >= TotalRead))
                {
                    NotifyFinalRead();
                }

                return read;
            }
            catch (Exception ex) {
                _onReadError(ex);

                throw;
            }
        }

        private void NotifyFirstRead()
        {
            if (_firstReadNotified)
                return;

            _firstReadNotified = true;

            _firstBytesRead();
        }

        private void NotifyFinalRead()
        {
            if (_finalReadNotified)
                return; 

            _finalReadNotified = true;

            _endRead(EndConnection, TotalRead);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_expectedLength != null && _expectedLength >= TotalRead)
            {
                NotifyFinalRead();
            }
        }
    }
}
