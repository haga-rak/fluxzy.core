using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class MetricsStreamCancellationTests
    {
        [Fact]
        public async Task ReadAsync_UsesParentTokenWhenRequestTokenNotCancelable()
        {
            using var parentCts = new CancellationTokenSource();
            parentCts.Cancel();

            var metricsStream = CreateMetricsStream(parentCts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await metricsStream.ReadAsync(new byte[1], CancellationToken.None).ConfigureAwait(false));
        }

        [Fact]
        public async Task ReadAsync_UsesRequestTokenWhenParentTokenNotCancelable()
        {
            using var requestCts = new CancellationTokenSource();
            requestCts.Cancel();

            var metricsStream = CreateMetricsStream(CancellationToken.None);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await metricsStream.ReadAsync(new byte[1], requestCts.Token).ConfigureAwait(false));
        }

        [Fact]
        public async Task ReadAsync_LinksTokensWhenBothAreCancelableAndDifferent()
        {
            using var parentCts = new CancellationTokenSource();
            using var requestCts = new CancellationTokenSource();
            parentCts.Cancel();

            var metricsStream = CreateMetricsStream(parentCts.Token);

            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                async () => await metricsStream.ReadAsync(new byte[1], requestCts.Token).ConfigureAwait(false));
        }

        private static MetricsStream CreateMetricsStream(CancellationToken parentToken)
        {
            return new MetricsStream(
                new PendingReadStream(),
                firstBytesRead: static () => { },
                endRead: static (_, _) => { },
                onReadError: static _ => { },
                endConnection: false,
                expectedLength: null,
                parentToken: parentToken);
        }

        private sealed class PendingReadStream : Stream
        {
            public override bool CanRead => true;

            public override bool CanSeek => false;

            public override bool CanWrite => false;

            public override long Length => throw new NotSupportedException();

            public override long Position {
                get => throw new NotSupportedException();
                set => throw new NotSupportedException();
            }

            public override void Flush()
            {
                throw new NotSupportedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                throw new NotSupportedException();
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

            public override async ValueTask<int> ReadAsync(
                Memory<byte> buffer,
                CancellationToken cancellationToken = default)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken).ConfigureAwait(false);
                return 0;
            }
        }
    }
}
