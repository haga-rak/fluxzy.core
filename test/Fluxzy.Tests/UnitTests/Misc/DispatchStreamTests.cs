// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc
{
    public class DispatchStreamTests
    {
        [Fact]
        public async Task ExistingConstructor_LeavesBaseStreamOpen()
        {
            var baseStream = new TrackingStream("base", new List<string>());
            var dispatch = new DispatchStream(baseStream, closeOnDone: true, Stream.Null);

            await dispatch.DisposeAsync();

            Assert.Equal(0, baseStream.DisposeCount);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task OwningMode_DisposesDestinationsBeforeBaseExactlyOnce(bool disposeAsync)
        {
            var disposalOrder = new List<string>();
            var baseStream = new TrackingStream("base", disposalOrder);
            var destination = new TrackingStream("destination", disposalOrder);
            var dispatch = new DispatchStream(
                baseStream, closeOnDone: true, DispatchStreamOwnership.OwnBaseStream, destination);

            if (disposeAsync)
                await dispatch.DisposeAsync();
            else
                dispatch.Dispose();

            dispatch.Dispose();
            await dispatch.DisposeAsync();

            Assert.Equal(1, destination.DisposeCount);
            Assert.Equal(1, baseStream.DisposeCount);
            Assert.Equal(new[] { "destination", "base" }, disposalOrder);
        }

        private sealed class TrackingStream : MemoryStream
        {
            private readonly string _name;
            private readonly List<string> _disposalOrder;
            private int _disposeCount;

            public TrackingStream(string name, List<string> disposalOrder)
            {
                _name = name;
                _disposalOrder = disposalOrder;
            }

            public int DisposeCount => Volatile.Read(ref _disposeCount);

            protected override void Dispose(bool disposing)
            {
                if (disposing && Interlocked.Increment(ref _disposeCount) == 1)
                    _disposalOrder.Add(_name);

                base.Dispose(disposing);
            }
        }
    }
}
