// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Concurrent;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Channels;
using PacketDotNet;
using SharpPcap;

namespace Fluxzy.Interop.Pcap
{
    internal class ConnectionQueue : IDisposable
    {
        private static readonly int LimitPacketNoSubscribe = 10; 
        private readonly Channel<RawCapture> _captureChannel = Channel.CreateUnbounded<RawCapture>();
        private int _totalPackedReceived;
        private volatile bool _subscribed;
        private ConnectionQueueWriter? _writer = null; 

        public long Key { get; }

        public ConnectionQueue(long key)
        {
            Key = key;
        }

        public IConnectionSubscription Subscribe(string outFileName)
        {
            if (_writer != null)
                throw new InvalidOperationException($"Already subscribed");

            _subscribed = true;

            return _writer = new ConnectionQueueWriter(Key, _captureChannel.Reader, outFileName);
        }

        public bool Post(RawCapture rawCapture)
        {
            Interlocked.Increment(ref _totalPackedReceived);

            if (!_subscribed && _totalPackedReceived > LimitPacketNoSubscribe)
                return false; // No subscriber on packet we leave
            return _captureChannel.Writer.TryWrite(rawCapture); 
        }
        
        public void Dispose()
        {
            var res = _captureChannel.Writer.TryComplete();
            Console.WriteLine($"complete called : {res}");
        }
    }

    /// <summary>
    /// Connection mode singleton 
    /// </summary>
    public class PacketQueue
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

        public void Unsubscribe(IConnectionSubscription subscription)
        {
            if (_captureChannels.TryRemove(subscription.Key, out var queue))
            {
                queue.Dispose();
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
    }
}