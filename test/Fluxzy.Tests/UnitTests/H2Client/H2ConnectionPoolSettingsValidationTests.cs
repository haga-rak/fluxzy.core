// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading.Tasks;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Frames;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    public class H2ConnectionPoolSettingsValidationTests
    {
        [Theory]
        [InlineData(16383)]
        [InlineData(16777216)]
        public async Task OutOfRangeMaxFrameSizeFaultsConnectionWithProtocolError(int value)
        {
            using var pipe = new DuplexPipe();
            var faulted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var pool = CreatePool(pipe, _ => faulted.TrySetResult(true));
            pool.Init();

            var bytes = H2FrameHelper.BuildSettingsFrame((SettingIdentifier.SettingsMaxFrameSize, value));
            await pipe.ServerWriteStream.WriteAsync(bytes);
            await pipe.ServerWriteStream.FlushAsync();

            await faulted.Task.WaitAsync(TimeSpan.FromSeconds(5));

            var snap = pool.SnapshotForTests();

            Assert.True(pool.Complete);
            Assert.Equal(H2ErrorCode.ProtocolError, snap.GoAwayErrorCode);
            Assert.Equal(0x4000, pool.Setting.Remote.MaxFrameSize);

            await pool.DisposeAsync();
        }

        [Fact]
        public async Task InRangeMaxFrameSizeIsApplied()
        {
            using var pipe = new DuplexPipe();
            var pool = CreatePool(pipe, _ => { });
            pool.Init();

            var bytes = H2FrameHelper.BuildSettingsFrame((SettingIdentifier.SettingsMaxFrameSize, 1024 * 1024));
            await pipe.ServerWriteStream.WriteAsync(bytes);
            await pipe.ServerWriteStream.FlushAsync();

            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (pool.Setting.Remote.MaxFrameSize != 1024 * 1024) {
                if (sw.Elapsed > TimeSpan.FromSeconds(5))
                    throw new TimeoutException("SETTINGS_MAX_FRAME_SIZE was not applied within 5s.");

                await Task.Delay(5);
            }

            Assert.False(pool.Complete);

            await pool.DisposeAsync();
        }

        private static H2ConnectionPool CreatePool(DuplexPipe pipe, Action<H2ConnectionPool> onFault)
        {
            var baseStream = new RecomposedStream(pipe.ClientReadStream, pipe.ClientWriteStream);
            var authority = new Authority("test.local", 443, true);

            return new H2ConnectionPool(
                baseStream,
                new H2StreamSetting(),
                authority,
                new Connection(authority, new TestIdProvider()),
                onConnectionFaulted: onFault);
        }
    }
}
