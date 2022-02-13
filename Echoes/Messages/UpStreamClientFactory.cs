using System;
using System.Threading.Tasks;
using Echoes.Core;

namespace Echoes
{
    //internal class UpStreamClientFactory : IUpStreamClientFactory
    //{
    //    private readonly IServerChannelPoolManager _poolManager;
    //    private readonly TunneledConnectionManager _tunneledConnectionManager;
    //    private readonly ProxyAlterationRule _proxyAlterationRule;
    //    private readonly IReferenceClock _referenceClock;

    //    public UpStreamClientFactory(
    //        IServerChannelPoolManager poolManager,
    //        TunneledConnectionManager tunneledConnectionManager,
    //        ProxyAlterationRule proxyAlterationRule,
    //        IReferenceClock referenceClock)
    //    {
    //        _poolManager = poolManager;
    //        _tunneledConnectionManager = tunneledConnectionManager;
    //        _proxyAlterationRule = proxyAlterationRule;
    //        _referenceClock = referenceClock;
    //    }

    //    public IUpstreamClient GetClientFor(Hrm requestMessage, Destination destination)
    //    {
    //        if (destination.DestinationType == DestinationType.Secure ||
    //            destination.DestinationType == DestinationType.Insecure)
    //        {
    //            // rule has hit the current request Message
    //            if (_proxyAlterationRule != null && _proxyAlterationRule.TryGetByRequestRule(requestMessage, out var replyContent))
    //            {
    //                return new DirectReplyUpStreamClient(replyContent);
    //            }
                
    //            return new Http11UpstreamClient(destination, _poolManager, _referenceClock); 
    //        }

    //        throw new InvalidOperationException();
    //    }

    //    public void CreateWebSocketTunnel(IDownStreamConnection connection, IUpstreamConnection upstreamConnection)
    //    {
    //        _tunneledConnectionManager.CreateTunnel(connection, upstreamConnection, true);
    //    }

    //    public async Task<bool> CreateBlindTunnel(Destination destination, IDownStreamConnection connection)
    //    {
    //        if (destination.DestinationType == DestinationType.BlindSecure)
    //        {
    //            var upStreamConnection = await _poolManager.CreateTunneledConnection(destination.Host, destination.Port).ConfigureAwait(false);
    //            _tunneledConnectionManager.CreateTunnel(connection, upStreamConnection, false);
    //            return true; 
    //        }

    //        return false;
    //    }
    //}
}