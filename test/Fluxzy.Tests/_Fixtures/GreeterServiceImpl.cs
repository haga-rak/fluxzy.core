using System.Collections.Generic;
using System.Threading.Tasks;
using Fluxzy.Tests.GrpcServices;
using Grpc.Core;

namespace Fluxzy.Tests._Fixtures;

public class GreeterServiceImpl : Greeter.GreeterBase
{
    public override async Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        if (request.Name == "error")
            throw new RpcException(new Status(StatusCode.Internal, "Simulated error"));

        if (request.Name == "slow")
            await Task.Delay(30_000, context.CancellationToken);

        // Echo request metadata in response trailers
        foreach (var entry in context.RequestHeaders)
        {
            if (entry.Key.StartsWith("x-test-"))
            {
                context.ResponseTrailers.Add(entry.Key, entry.Value);
            }
        }

        return new HelloReply { Message = $"Hello {request.Name}" };
    }

    public override async Task SayHelloServerStream(
        HelloRequest request, IServerStreamWriter<HelloReply> responseStream, ServerCallContext context)
    {
        if (request.Name == "error")
            throw new RpcException(new Status(StatusCode.Internal, "Simulated error"));

        for (var i = 0; i < 5; i++)
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            await responseStream.WriteAsync(new HelloReply { Message = $"Hello {request.Name} #{i}" });
        }
    }

    public override async Task<HelloReply> SayHelloClientStream(
        IAsyncStreamReader<HelloRequest> requestStream, ServerCallContext context)
    {
        var names = new List<string>();

        await foreach (var request in requestStream.ReadAllAsync())
        {
            names.Add(request.Name);
        }

        return new HelloReply { Message = $"Hello {string.Join(", ", names)}" };
    }

    public override async Task SayHelloBidiStream(
        IAsyncStreamReader<HelloRequest> requestStream,
        IServerStreamWriter<HelloReply> responseStream,
        ServerCallContext context)
    {
        await foreach (var request in requestStream.ReadAllAsync())
        {
            await responseStream.WriteAsync(new HelloReply { Message = $"Hello {request.Name}" });
        }
    }
}
