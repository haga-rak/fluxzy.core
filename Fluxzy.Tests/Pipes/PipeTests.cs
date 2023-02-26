using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Interop.Pcap;
using Fluxzy.Interop.Pcap.Cli;
using Fluxzy.Interop.Pcap.Cli.Clients;
using Fluxzy.Interop.Pcap.Messages;
using Xunit;

namespace Fluxzy.Tests.Pipes
{
    //public class PipeTests
    //{
    //    // [Fact]
    //    public async Task TestPipeClientServer()
    //    {
    //        var proxyScope = new ProxyScope(() => new FluxzyNetOutOfProcessHost(), (a) => new OutOfProcessCaptureContext(a));
    //        var tokenSource = new CancellationTokenSource(10000); 
    //        var expectedKey = 99;
    //        var receivedKey = -1L;

    //        var expectedSubscribeMessage = new SubscribeMessage(IPAddress.Parse("195.23.56.21"), 985, 123, "toto.txt"); 
    //        var expectedUnSubscribe = new UnsubscribeMessage(expectedKey);
    //        var includeMessage = new IncludeMessage(IPAddress.Parse("8.56.3.2"), 4531);

    //        SubscribeMessage receivedSubscribeMessage = default;
    //        UnsubscribeMessage receivedUnsubscribeMessage = default;
    //        IncludeMessage receivedIncludeMessage = default;

    //        var pipeMessageReceiver = new PipeMessageReceiver(m =>
    //            {
    //                receivedSubscribeMessage = m; 
    //                return expectedKey;
    //            },
    //            m =>
    //            {
    //                receivedUnsubscribeMessage = m; 
    //            },
    //            m =>
    //            {
    //                receivedIncludeMessage = m; 
    //            },
    //            () =>
    //            {

    //            },
    //            tokenSource.Token
    //            );

    //        using (var pipeClient = await OutOfProcessCaptureContext.CreateAndConnect(proxyScope)) {

    //            pipeClient.Include(includeMessage.RemoteAddress, includeMessage.RemotePort);
    //            receivedKey = pipeClient.Subscribe(expectedSubscribeMessage.OutFileName, expectedSubscribeMessage.RemoteAddress, expectedSubscribeMessage.RemotePort, expectedSubscribeMessage.LocalPort);
    //            await pipeClient.Unsubscribe(receivedKey);
    //        }

    //        await pipeMessageReceiver.WaitForExit();

    //        Assert.Equal(expectedSubscribeMessage, receivedSubscribeMessage);
    //        Assert.Equal(expectedUnSubscribe, receivedUnsubscribeMessage);
    //        Assert.Equal(includeMessage, receivedIncludeMessage);
    //        Assert.Equal(expectedKey, receivedKey);
    //    }
    //}
}
