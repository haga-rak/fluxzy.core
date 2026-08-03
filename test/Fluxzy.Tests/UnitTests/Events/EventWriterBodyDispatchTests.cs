using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.UnitTests.Substitutions.Actions;
using Fluxzy.Writers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Events;

public class EventWriterBodyDispatchTests
{
    [Fact]
    public void Writers_DeclareWhetherTheyCaptureBodyContent()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"fluxzy-writer-{Guid.NewGuid():N}");

        try
        {
            Assert.False(new EventOnlyArchiveWriter().CapturesBodyContent);
            Assert.True(new DirectoryArchiveWriter(directory, null).CapturesBodyContent);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    [Fact]
    public async Task DefaultWriter_CompletesOnceAfterSuccessfulBodyForwarding()
    {
        var requestBody = Encoding.UTF8.GetBytes("request-body");
        var responseBody = Enumerable.Range(0, 8192).Select(i => (byte) (i % 251)).ToArray();
        var requestReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstResponseHalfSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseResponse = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var setup = await ProxiedHostSetup.Create(configureRoutes: app =>
        {
            app.MapPost("/success", async context =>
            {
                using var request = new MemoryStream();
                await context.Request.Body.CopyToAsync(request);
                requestReceived.TrySetResult(request.ToArray());

                context.Response.ContentLength = responseBody.Length;
                await context.Response.Body.WriteAsync(responseBody.AsMemory(0, responseBody.Length / 2));
                await context.Response.Body.FlushAsync();
                firstResponseHalfSent.TrySetResult();
                await releaseResponse.Task;
                await context.Response.Body.WriteAsync(responseBody.AsMemory(responseBody.Length / 2));
            });
        });

        var updates = new ConcurrentQueue<ArchiveUpdateType>();
        var completed = new TaskCompletionSource<Exchange>(TaskCreationOptions.RunContinuationsAsynchronously);

        setup.Proxy.Writer.ExchangeUpdated += (_, args) =>
        {
            updates.Enqueue(args.UpdateType);

            if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                completed.TrySetResult(args.Original);
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "/success")
        {
            Content = new ByteArrayContent(requestBody)
        };
        using var response = await setup.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

        await firstResponseHalfSent.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.Equal(requestBody, await requestReceived.Task.WaitAsync(TimeSpan.FromSeconds(10)));
        Assert.Equal(
            new[] { ArchiveUpdateType.BeforeRequestHeader, ArchiveUpdateType.AfterResponseHeader },
            updates.ToArray());
        Assert.Equal(0, setup.Proxy.Writer.TotalProcessedExchanges);

        releaseResponse.TrySetResult();

        Assert.Equal(responseBody, await response.Content.ReadAsByteArrayAsync());
        var completedExchange = await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(
            new[]
            {
                ArchiveUpdateType.BeforeRequestHeader,
                ArchiveUpdateType.AfterResponseHeader,
                ArchiveUpdateType.AfterResponse
            },
            updates.ToArray());
        Assert.Equal(1, setup.Proxy.Writer.TotalProcessedExchanges);
        Assert.Null(completedExchange.Request.Body);
        Assert.Null(completedExchange.Response.Body);
    }

    [Fact]
    public async Task DefaultWriter_DoesNotCompleteFaultedResponse()
    {
        await using var setup = await ProxiedHostSetup.Create(
            setting => setting.AddAlterationRulesForAny(
                new AddResponseBodyStreamSubstitutionAction(new FaultingSubstitution())),
            app => app.MapGet("/fault", () => "original-response"));

        var faultedExchangeId = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedIds = new ConcurrentBag<int>();

        setup.Proxy.Writer.ExchangeUpdated += (_, args) =>
        {
            if (args.UpdateType == ArchiveUpdateType.AfterResponseHeader &&
                args.ExchangeInfo.FullUrl.EndsWith("/fault", StringComparison.Ordinal))
                faultedExchangeId.TrySetResult(args.Original.Id);

            if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                completedIds.Add(args.Original.Id);
        };

        await Assert.ThrowsAnyAsync<HttpRequestException>(() => setup.Client.GetAsync("/fault"));
        var faultedId = await faultedExchangeId.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.DoesNotContain(faultedId, completedIds);
        Assert.Equal(0, setup.Proxy.Writer.TotalProcessedExchanges);
    }

    [Fact]
    public async Task DefaultWriter_DoesNotCompleteCancelledResponse()
    {
        var firstChunkSent = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseBody = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var originStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var chunk = new byte[64 * 1024];

        await using var setup = await ProxiedHostSetup.Create(configureRoutes: app =>
        {
            app.MapGet("/cancel", async context =>
            {
                context.Response.ContentLength = 128L * 1024 * 1024;

                try
                {
                    await context.Response.Body.WriteAsync(chunk);
                    await context.Response.Body.FlushAsync();
                    firstChunkSent.TrySetResult();
                    await releaseBody.Task;

                    for (var i = 1; i < 2048; i++)
                    {
                        await context.Response.Body.WriteAsync(chunk, context.RequestAborted);
                        await context.Response.Body.FlushAsync(context.RequestAborted);
                    }
                }
                catch (OperationCanceledException)
                {
                }
                catch (IOException)
                {
                }
                finally
                {
                    originStopped.TrySetResult();
                }
            });
            app.MapGet("/barrier", () => "ok");
        });

        var cancelledExchangeId = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var completedIds = new ConcurrentBag<int>();

        setup.Proxy.Writer.ExchangeUpdated += (_, args) =>
        {
            if (args.UpdateType == ArchiveUpdateType.AfterResponseHeader &&
                args.ExchangeInfo.FullUrl.EndsWith("/cancel", StringComparison.Ordinal))
                cancelledExchangeId.TrySetResult(args.Original.Id);

            if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                completedIds.Add(args.Original.Id);
        };

        using (var response = await setup.Client.GetAsync("/cancel", HttpCompletionOption.ResponseHeadersRead))
        {
            await firstChunkSent.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        releaseBody.TrySetResult();
        var cancelledId = await cancelledExchangeId.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await originStopped.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal("ok", await setup.Client.GetStringAsync("/barrier"));

        Assert.DoesNotContain(cancelledId, completedIds);
        Assert.Equal(1, setup.Proxy.Writer.TotalProcessedExchanges);
    }

    [Fact]
    public async Task DefaultWriter_PreservesResponseSubstitution()
    {
        const string expected = "substituted-response";

        await using var setup = await ProxiedHostSetup.Create(
            setting => setting.AddAlterationRulesForAny(
                new AddResponseBodyStreamSubstitutionAction(
                    new ReturnsContentLengthSubstitution(expected))),
            app => app.MapGet("/substitute", () => "original-response"));

        var updates = new ConcurrentQueue<ArchiveUpdateType>();
        var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        setup.Proxy.Writer.ExchangeUpdated += (_, args) =>
        {
            updates.Enqueue(args.UpdateType);

            if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                completed.TrySetResult();
        };

        Assert.Equal(expected, await setup.Client.GetStringAsync("/substitute"));
        await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(
            new[]
            {
                ArchiveUpdateType.BeforeRequestHeader,
                ArchiveUpdateType.AfterResponseHeader,
                ArchiveUpdateType.AfterResponse
            },
            updates.ToArray());
    }

    [Fact]
    public async Task DirectoryWriter_CapturesExactRequestAndResponseBytes()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"fluxzy-capture-{Guid.NewGuid():N}");
        var requestBody = Enumerable.Range(0, 4096).Select(i => (byte) (i % 239)).ToArray();
        var responseBody = Enumerable.Range(0, 8192).Select(i => (byte) (255 - i % 241)).ToArray();
        var requestReceived = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);

        try
        {
            await using var setup = await ProxiedHostSetup.Create(
                setting => setting.SetArchivingPolicy(ArchivingPolicy.CreateFromDirectory(directory)),
                app => app.MapPost("/capture", async context =>
                {
                    using var request = new MemoryStream();
                    await context.Request.Body.CopyToAsync(request);
                    requestReceived.TrySetResult(request.ToArray());

                    context.Response.ContentLength = responseBody.Length;
                    await context.Response.Body.WriteAsync(responseBody);
                }));

            var completed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            setup.Proxy.Writer.ExchangeUpdated += (_, args) =>
            {
                if (args.UpdateType == ArchiveUpdateType.AfterResponse)
                    completed.TrySetResult();
            };

            using var content = new ByteArrayContent(requestBody);
            using var response = await setup.Client.PostAsync("/capture", content);

            Assert.Equal(responseBody, await response.Content.ReadAsByteArrayAsync());
            Assert.Equal(requestBody, await requestReceived.Task.WaitAsync(TimeSpan.FromSeconds(10)));
            await completed.Task.WaitAsync(TimeSpan.FromSeconds(10));

            var requestPath = Assert.Single(Directory.GetFiles(Path.Combine(directory, "contents"), "req-*.data"));
            var responsePath = Assert.Single(Directory.GetFiles(Path.Combine(directory, "contents"), "res-*.data"));

            Assert.Equal(requestBody, await File.ReadAllBytesAsync(requestPath));
            Assert.Equal(responseBody, await File.ReadAllBytesAsync(responsePath));
            Assert.Equal(1, setup.Proxy.Writer.TotalProcessedExchanges);
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }

    private sealed class FaultingSubstitution : IStreamSubstitution
    {
        public ValueTask<Stream> Substitute(Stream originalStream)
        {
            return ValueTask.FromResult<Stream>(new FaultingReadStream());
        }
    }

    private sealed class FaultingReadStream : MemoryStream
    {
        private bool _readOnce;

        public FaultingReadStream()
            : base(new byte[8192])
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (_readOnce)
                throw new IOException("Injected response body read failure.");

            _readOnce = true;
            return base.Read(buffer, offset, Math.Min(count, 256));
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_readOnce)
                return ValueTask.FromException<int>(new IOException("Injected response body read failure."));

            _readOnce = true;
            return base.ReadAsync(buffer[..Math.Min(buffer.Length, 256)], cancellationToken);
        }
    }
}
