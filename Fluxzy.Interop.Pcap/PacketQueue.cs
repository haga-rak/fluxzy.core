// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using SharpPcap;

namespace Fluxzy.Interop.Pcap
{
    /// <summary>
    /// Connection mode singleton 
    /// </summary>
    public class PacketQueue : IAsyncDisposable
    {
        private readonly HashSet<Authority> _allowedAuthorityKeys = new();
        private readonly ConcurrentDictionary<long, ConnectionQueue> _captureChannels = new(); 

        public void Include(IPAddress remoteAddress, int remotePort)
        {
            lock (_allowedAuthorityKeys)
                _allowedAuthorityKeys.Add(new Authority(remoteAddress, remotePort)); 
        }

        public IConnectionSubscription Subscribe(string outFileName, IPAddress remoteAddress, int remotePort, int localPort)
        {
            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddress);
            var queue = _captureChannels.GetOrAdd(connectionKey, (ck) => new ConnectionQueue(ck));
            return queue.Subscribe(outFileName);
        }

        public async ValueTask Unsubscribe(IConnectionSubscription subscription)
        {
            if (_captureChannels.TryRemove(subscription.Key, out var queue))
            {
                await queue.DisposeAsync();
            }
        }

        public void Enqueue(RawCapture rawCapture, EthernetPacket ethernetPacket, PhysicalAddress localPhysicalAddress)
        {
            var sendPacket = Equals(ethernetPacket.SourceHardwareAddress, localPhysicalAddress);
            var ipPacket = ethernetPacket.Extract<IPPacket>();
            var transportPacket = ipPacket.Extract<TransportPacket>();

            if (transportPacket == null)
                return;

            var remoteAddress = sendPacket ? ipPacket.DestinationAddress : ipPacket.SourceAddress;
            var remotePort = sendPacket ? transportPacket.DestinationPort : transportPacket.SourcePort;

            var authority = new Authority(remoteAddress, remotePort);

            lock (_allowedAuthorityKeys)
            {
                if (!_allowedAuthorityKeys.Contains(authority))
                    return; // Packet ignored 
            }

            var localPort = sendPacket ? transportPacket.SourcePort : transportPacket.DestinationPort;

            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddress);

            var queue = _captureChannels.GetOrAdd(connectionKey, (ck => new ConnectionQueue(ck)));

            if (!queue.Post(rawCapture))
            {
                //  TODO : remove because no one registered to this queue yet 
                _captureChannels.TryRemove(connectionKey, out _);
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            await Task.WhenAll(_captureChannels.Values.Select(v => v.DisposeAsync().AsTask()));
        }
    }
}