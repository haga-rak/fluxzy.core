// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Misc.Streams
{
    /// <summary>
    ///     Read-only pass-through that bounds the time between two successive reads.
    ///     The deadline is re-armed before each read and disarmed after, so a slow but
    ///     steady stream never trips it. On expiry the provided callback must unblock
    ///     the pending inner read out of band (closing the transport, cancelling a pipe
    ///     read), after which the failure surfaces as a timeout IOException.
    /// </summary>
    internal sealed class ReadIdleTimeoutStream : Stream
    {
        private readonly Stream _inner;
        private readonly TimeSpan _idleTimeout;
        private readonly string _timeoutMessage;
        private readonly CancellationTokenSource _timeoutCts = new();

        private volatile bool _timedOut;

        public ReadIdleTimeoutStream(
            Stream inner, TimeSpan idleTimeout, string timeoutMessage, Action onTimeout)
        {
            _inner = inner;
            _idleTimeout = idleTimeout;
            _timeoutMessage = timeoutMessage;

            _timeoutCts.Token.UnsafeRegister(static state => {
                var (stream, callback) = ((ReadIdleTimeoutStream, Action)) state!;
                stream._timedOut = true;
                callback();
            }, (this, onTimeout));
        }

        public override bool CanRead => _inner.CanRead;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _timeoutCts.CancelAfter(_idleTimeout);

            try {
                return await _inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (_timedOut) {
                throw new IOException(_timeoutMessage, ex);
            }
            finally {
                _timeoutCts.CancelAfter(Timeout.InfiniteTimeSpan);
            }
        }

        public override Task<int> ReadAsync(
            byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            return ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            _timeoutCts.CancelAfter(_idleTimeout);

            try {
                return _inner.Read(buffer, offset, count);
            }
            catch (Exception ex) when (_timedOut) {
                throw new IOException(_timeoutMessage, ex);
            }
            finally {
                _timeoutCts.CancelAfter(Timeout.InfiniteTimeSpan);
            }
        }

        public override void Flush()
        {
        }

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                _timeoutCts.Dispose();
                _inner.Dispose();
            }

            base.Dispose(disposing);
        }

        public override async ValueTask DisposeAsync()
        {
            _timeoutCts.Dispose();
            await _inner.DisposeAsync().ConfigureAwait(false);
            await base.DisposeAsync().ConfigureAwait(false);
        }
    }
}
