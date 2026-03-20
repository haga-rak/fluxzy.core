// gRPC requires HTTP/2. When Fluxzy acts as a MITM proxy, it terminates
// the client TLS connection and re-establishes a new one to the remote server.
// By default, Fluxzy serves HTTP/1.1 on the proxy-to-client leg.
// SetServeH2(true) makes Fluxzy serve HTTP/2 to the client, which is
// mandatory for gRPC because gRPC relies on HTTP/2 framing (streams,
// trailers, binary frames). Without it, the gRPC client will fail to
// negotiate the protocol.

using System.Net;
using Fluxzy;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Filters;
using Grpc.Core;
using Grpc.Net.Client;

namespace Samples.No023.GrpcThroughProxy
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var fluxzySetting = FluxzySetting.CreateLocalRandomPort();

            // Enable HTTP/2 on the proxy-to-client connection
            fluxzySetting.SetServeH2(true);

            // Skip remote certificate validation (needed for MITM mode)
            fluxzySetting.AddAlterationRules(
                new SkipRemoteCertificateValidationAction(), AnyFilter.Default);

            await using var proxy = new Proxy(fluxzySetting);
            var endpoints = proxy.Run();

            var proxyPort = endpoints.First().Port;

            var handler = new HttpClientHandler
            {
                Proxy = new WebProxy($"http://127.0.0.1:{proxyPort}"),
                UseProxy = true,
                // Trust the Fluxzy-generated certificate
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            using var channel = GrpcChannel.ForAddress("https://grpcb.in:9001", new GrpcChannelOptions
            {
                HttpHandler = handler
            });

            var client = new HelloService.HelloServiceClient(channel);

            // Unary call
            var reply = await client.SayHelloAsync(new HelloRequest { Greeting = "fluxzy" });
            Console.WriteLine($"Unary reply: {reply.Reply}");

            // Server streaming call
            using var streamCall = client.LotsOfReplies(new HelloRequest { Greeting = "stream-test" });

            Console.WriteLine("Server streaming replies:");

            await foreach (var streamReply in streamCall.ResponseStream.ReadAllAsync())
            {
                Console.WriteLine($"  {streamReply.Reply}");
            }
        }
    }
}
