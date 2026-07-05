// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Threading;
using Fluxzy.Clients;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;
using Fluxzy.Core;
using Xunit;

namespace Fluxzy.Tests.UnitTests.H2Client
{
    public class StreamWorkerWindowUpdateTests
    {
        private const int FrameSize = 16 * 1024;

        [Fact]
        public void ResponseConsumption_UsesHalfLocalWindowForStreamWindowUpdates()
        {
            var setting = new H2StreamSetting();
            var recorder = new WindowUpdateRecorder();

            using var fixture = CreateWorker(setting, recorder.Record);

            Consume(fixture.Worker, 10 * 1024 * 1024);

            Assert.Equal(3, recorder.StreamWindowUpdates);
            Assert.Equal(9 * 1024 * 1024, recorder.StreamWindowUpdateBytes);
        }

        [Fact]
        public void ResponseConsumption_PreservesSmallWindowReplenishment()
        {
            var setting = new H2StreamSetting();
            setting.Local.WindowSize = 64 * 1024;

            var recorder = new WindowUpdateRecorder();

            using var fixture = CreateWorker(setting, recorder.Record);

            Consume(fixture.Worker, 64 * 1024);

            Assert.Equal(2, recorder.StreamWindowUpdates);
            Assert.Equal(64 * 1024, recorder.StreamWindowUpdateBytes);
        }

        [Fact]
        public void ResponseConsumption_PreservesOneKiBWindowReplenishment()
        {
            var setting = new H2StreamSetting();
            setting.Local.WindowSize = 1024;

            var recorder = new WindowUpdateRecorder();

            using var fixture = CreateWorker(setting, recorder.Record);

            Consume(fixture.Worker, 4 * 1024, frameSize: 1024);

            Assert.Equal(4, recorder.StreamWindowUpdates);
            Assert.Equal(4 * 1024, recorder.StreamWindowUpdateBytes);
        }

        private static WorkerFixture CreateWorker(H2StreamSetting setting, UpStreamChannel upStreamChannel)
        {
            var authority = new Authority("test.local", 443, true);
            var memoryProvider = ArrayPoolMemoryProvider<char>.Default;
            var hpackEncoder = new HPackEncoder(new EncodingContext(memoryProvider));
            var hpackDecoder = new HPackDecoder(new DecodingContext(authority, memoryProvider));
            var headerEncoder = new HeaderEncoder(hpackEncoder, hpackDecoder, setting);
            var overallWindow = new WindowSizeHolder(setting.OverallWindowSize, 0);

            var context = new StreamContext(
                connectionId: 1,
                authority: authority,
                setting: setting,
                headerEncoder: headerEncoder,
                upStreamChannel: upStreamChannel,
                overallWindowSizeHolder: overallWindow);

            var streamPool = new StreamPool(context);
            var resetTokenSource = new CancellationTokenSource();
            var exchange = MakeExchange(authority);

            var worker = new StreamWorker(1, streamPool, exchange, resetTokenSource);

            return new WorkerFixture(worker, streamPool, overallWindow, resetTokenSource);
        }

        private static void Consume(StreamWorker worker, int responseBytes, int frameSize = FrameSize)
        {
            var remaining = responseBytes;

            while (remaining > 0) {
                var dataSize = Math.Min(frameSize, remaining);
                worker.OnDataConsumedByCaller(dataSize);
                remaining -= dataSize;
            }
        }

        private static Exchange MakeExchange(Authority authority)
        {
            var header = "GET / HTTP/2.0\r\nhost: test.local\r\n\r\n".AsMemory();
            return new Exchange(IIdProvider.FromZero, authority, header, "HTTP/2", DateTime.UtcNow);
        }

        private class WindowUpdateRecorder
        {
            public int StreamWindowUpdates { get; private set; }

            public int StreamWindowUpdateBytes { get; private set; }

            public void Record(ref WriteTask writeTask)
            {
                if (writeTask.FrameType != H2FrameType.WindowUpdate || writeTask.StreamIdentifier == 0)
                    return;

                StreamWindowUpdates++;
                StreamWindowUpdateBytes += writeTask.WindowUpdateSize;
            }
        }

        private sealed class WorkerFixture : IDisposable
        {
            private readonly StreamPool _streamPool;
            private readonly WindowSizeHolder _overallWindow;
            private readonly CancellationTokenSource _resetTokenSource;

            public WorkerFixture(
                StreamWorker worker,
                StreamPool streamPool,
                WindowSizeHolder overallWindow,
                CancellationTokenSource resetTokenSource)
            {
                Worker = worker;
                _streamPool = streamPool;
                _overallWindow = overallWindow;
                _resetTokenSource = resetTokenSource;
            }

            public StreamWorker Worker { get; }

            public void Dispose()
            {
                Worker.Dispose();
                _streamPool.Dispose();
                _overallWindow.Dispose();
                _resetTokenSource.Dispose();
            }
        }
    }
}
