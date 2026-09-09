// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Misc;

public class MetricsStreamDispositionTests
{
    [Theory]
    [InlineData(10, 2, true)]
    [InlineData(-1, 2, true)]
    [InlineData(2, 2, false)]
    public async Task DisposalCompletesOnceWithCorrectConnectionDisposition(
        long expectedLength, int bytesToRead, bool expectedClose)
    {
        var completionCount = 0;
        var closeConnection = false;
        var stream = new MetricsStream(
            new MemoryStream(new byte[10]),
            () => { },
            (close, _) => {
                completionCount++;
                closeConnection = close;
            },
            _ => { },
            endConnection: false,
            expectedLength: expectedLength < 0 ? null : expectedLength,
            parentToken: CancellationToken.None);
        var buffer = new byte[bytesToRead];

        Assert.Equal(bytesToRead, await stream.ReadAsync(buffer));
        await stream.DisposeAsync();
        stream.Dispose();

        Assert.Equal(1, completionCount);
        Assert.Equal(expectedClose, closeConnection);
    }

    [Fact]
    public async Task OwningDispatchPropagatesPartialBodyCloseDisposition()
    {
        var completed = false;
        var closeConnection = false;
        var metrics = new MetricsStream(
            new MemoryStream(new byte[10]),
            () => { },
            (close, _) => {
                completed = true;
                closeConnection = close;
            },
            _ => { },
            endConnection: false,
            expectedLength: 10,
            parentToken: CancellationToken.None);
        var dispatch = new DispatchStream(
            metrics, closeOnDone: true, DispatchStreamOwnership.OwnBaseStream, Stream.Null);
        var buffer = new byte[2];

        Assert.Equal(2, await dispatch.ReadAsync(buffer));
        await dispatch.DisposeAsync();

        Assert.True(completed);
        Assert.True(closeConnection);
    }
}
