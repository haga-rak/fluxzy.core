using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Tests._Fixtures;
using Fluxzy.Tests.GrpcServices;
using Grpc.Core;
using Xunit;

namespace Fluxzy.Tests.Cases;

public class GrpcIntegrationTests
{
    // Tier 1 - Core

    [Fact]
    public async Task Unary_RPC_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        var reply = await client.SayHelloAsync(new HelloRequest { Name = "World" });

        Assert.Equal("Hello World", reply.Message);
    }

    [Fact]
    public async Task ServerStreaming_RPC_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        var replies = new List<string>();

        using var call = client.SayHelloServerStream(new HelloRequest { Name = "World" });

        await foreach (var reply in call.ResponseStream.ReadAllAsync())
        {
            replies.Add(reply.Message);
        }

        Assert.Equal(5, replies.Count);

        for (var i = 0; i < 5; i++)
        {
            Assert.Equal($"Hello World #{i}", replies[i]);
        }
    }

    [Fact]
    public async Task ClientStreaming_RPC_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        using var call = client.SayHelloClientStream();

        await call.RequestStream.WriteAsync(new HelloRequest { Name = "Alice" });
        await call.RequestStream.WriteAsync(new HelloRequest { Name = "Bob" });
        await call.RequestStream.WriteAsync(new HelloRequest { Name = "Charlie" });
        await call.RequestStream.CompleteAsync();

        var reply = await call.ResponseAsync;

        Assert.Equal("Hello Alice, Bob, Charlie", reply.Message);
    }

    [Fact]
    public async Task BidiStreaming_RPC_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        using var call = client.SayHelloBidiStream();

        var names = new[] { "Alice", "Bob", "Charlie" };
        var replies = new List<string>();

        foreach (var name in names)
        {
            await call.RequestStream.WriteAsync(new HelloRequest { Name = name });
        }

        await call.RequestStream.CompleteAsync();

        await foreach (var reply in call.ResponseStream.ReadAllAsync())
        {
            replies.Add(reply.Message);
        }

        Assert.Equal(3, replies.Count);
        Assert.Equal("Hello Alice", replies[0]);
        Assert.Equal("Hello Bob", replies[1]);
        Assert.Equal("Hello Charlie", replies[2]);
    }

    [Fact]
    public async Task GrpcError_Status_Forwarded_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.SayHelloAsync(new HelloRequest { Name = "error" }));

        Assert.Equal(StatusCode.Internal, ex.StatusCode);
        Assert.Contains("Simulated error", ex.Status.Detail);
    }

    [Fact]
    public async Task GrpcMetadata_Headers_Forwarded_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        var metadata = new Metadata
        {
            { "x-test-custom", "my-value" }
        };

        var call = client.SayHelloAsync(new HelloRequest { Name = "World" }, metadata);
        var reply = await call.ResponseAsync;

        Assert.Equal("Hello World", reply.Message);

        var trailers = call.GetTrailers();
        var customTrailer = trailers.FirstOrDefault(t => t.Key == "x-test-custom");
        Assert.NotNull(customTrailer);
        Assert.Equal("my-value", customTrailer.Value);
    }

    // Tier 2 - Robustness

    [Fact]
    public async Task LargeMessage_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        // Use a message close to the default H2 max frame size (16KB) to stress gRPC framing
        var largeName = new string('A', 12 * 1024);
        var reply = await client.SayHelloAsync(new HelloRequest { Name = largeName });

        Assert.Equal($"Hello {largeName}", reply.Message);
    }

    [Fact]
    public async Task ConcurrentUnary_RPCs_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            var reply = await client.SayHelloAsync(new HelloRequest { Name = $"User{i}" });
            return reply.Message;
        });

        var results = await Task.WhenAll(tasks);

        for (var i = 0; i < 10; i++)
        {
            Assert.Equal($"Hello User{i}", results[i]);
        }
    }

    [Fact]
    public async Task Deadline_Propagation_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        // Server delays 30s for "slow", deadline is 1s - should get DeadlineExceeded
        var deadline = DateTime.UtcNow.AddSeconds(1);

        var ex = await Assert.ThrowsAsync<RpcException>(async () =>
            await client.SayHelloAsync(
                new HelloRequest { Name = "slow" },
                deadline: deadline));

        Assert.Equal(StatusCode.DeadlineExceeded, ex.StatusCode);
    }

    // Tier 3 - Edge cases

    [Fact]
    public async Task EmptyMessage_Through_Proxy()
    {
        await using var setup = await GrpcProxiedSetup.Create();
        var client = new Greeter.GreeterClient(setup.Channel);

        var reply = await client.SayHelloAsync(new HelloRequest { Name = "" });

        Assert.Equal("Hello ", reply.Message);
    }
}
