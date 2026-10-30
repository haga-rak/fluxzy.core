// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Concurrent;
using System.Net;
using Fluxzy.Core.Pcap.Reading;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Core.Pcap
{
    public class DirectCaptureContext : ICaptureContext
    {
        private readonly List<CaptureSource> _sources = new();

        private readonly ConcurrentDictionary<long, object?> _knownAuthorities = new();
        private bool _disposed;

        private bool _halted;
        private SyncWriterQueue? _packetQueue;

        public DirectCaptureContext(IPAddress? localAddress = null)
        {
            var localAddress1 = localAddress ?? IpUtility.GetDefaultRouteV4Address();

            var devices = CaptureDeviceList.Instance.OfType<PcapDevice>().ToList();

            // TODO: use a better heuristic to select the capture device
            // TODO: demand the interface name as parameter

            var physicalDevice = devices
                                 .Where(l => !l.IsLoopback())
                                 .OrderByDescending(l => l.IsUp())
                                 .ThenByDescending(l => l.IsRunning())
                                 .ThenByDescending(l => l.IsConnected())
                                 .ThenByDescending(d => d.Interface.Addresses.Any(
                                     a => Equals(a.Addr.ipAddress, localAddress1)))
                                 .ThenByDescending(d =>
                                     d.Interface.GatewayAddresses.Any(g => !g.IsIPv6LinkLocal))
                                 .FirstOrDefault();

            var loopbackDevice = devices.FirstOrDefault(l => l.IsLoopback());

            if (physicalDevice == null && loopbackDevice == null)
                throw new InvalidOperationException("No capture device available");

            if (physicalDevice != null)
                _sources.Add(new CaptureSource(physicalDevice, ReadMac(physicalDevice), this));

            if (loopbackDevice != null)
                _sources.Add(new CaptureSource(loopbackDevice, null, this));
        }

        private static long? ReadMac(PcapDevice device)
        {
            try {
                var bytes = device.MacAddress?.GetAddressBytes();

                if (bytes == null || bytes.Length != 6)
                    return null;

                var mac = NetUtility.MacToLong(bytes);

                return mac == 0 ? null : mac;
            }
            catch {
                return null;
            }
        }

        public void Include(IPAddress remoteAddress, int remotePort)
        {
            _knownAuthorities.TryAdd(PacketKeyBuilder.GetAuthorityKey(remoteAddress, remotePort), null);
        }

        public long Subscribe(
            string outFileName,
            IPAddress remoteAddress, int remotePort, int localPort)
        {
            if (_packetQueue == null)
                throw new InvalidOperationException("Not started yet");

            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddress);

            // The OS can reuse an ephemeral local port while the previous writer is still in
            // its linger window, which would collide on connectionKey. Rotate replaces such a
            // registered stale writer but keeps an unregistered one so the packets captured
            // before Subscribe (SYN, SYN-ACK, ACK) are preserved.
            var writer = _packetQueue.Rotate(connectionKey);

            writer.Register(outFileName);

            return writer.SubscriptionId;
        }

        public void StoreKey(string nssKey, IPAddress remoteAddress, int remotePort, int localPort)
        {
            if (_packetQueue == null)
                throw new InvalidOperationException("Not started yet");

            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddress);
            var writer = _packetQueue.GetOrAdd(connectionKey);

            writer.StoreKey(nssKey);
        }

        public void Flush()
        {
            if (_packetQueue == null)
                return;

            _packetQueue.FlushAll();
        }

        public void ClearAll()
        {
            if (_packetQueue == null)
                return;

            _packetQueue.ClearAll();
        }

        public ValueTask Unsubscribe(long subscription)
        {
            if (_packetQueue == null)
                throw new InvalidOperationException("Not started yet");

            _packetQueue.TryRemoveBySubId(subscription, out _);

            return default;
        }

        public bool Available {
            get
            {
                try {
                    return CaptureDeviceList.Instance.OfType<PcapDevice>().Any();
                }
                catch {
                    // ignore further warning 

                    return false;
                }
            }
        }

        public Task Start()
        {
            _packetQueue = new SyncWriterQueue();

            lock (this) {
                if (_disposed)
                    return Task.CompletedTask;

                Exception? lastError = null;

                foreach (var source in _sources.ToList()) {
                    try {
                        source.Start();
                    }
                    catch (Exception ex) {
                        lastError = ex;
                        _sources.Remove(source);
                        source.Dispose();
                    }
                }

                if (_sources.Count == 0 && lastError != null)
                    throw lastError;
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            lock (this) {
                if (_disposed)
                    return default;

                _disposed = true;

                Stop();

                foreach (var source in _sources) {
                    source.Dispose();
                }

                _packetQueue?.Dispose();
            }

            return default;
        }

        private void Stop()
        {
            if (_halted)
                return;

            _halted = true;

            foreach (var source in _sources) {
                source.Stop();
            }
        }

        private void OnPacketArrival(CaptureSource source, ref PacketCapture capture)
        {
            if (_packetQueue == null)
                return;

            var data = capture.Data;
            bool? sentPacket = null;

            switch (capture.Device.LinkType) {
                case LinkLayers.Ethernet: {
                    var ethernetPacketInfo = new EthernetPacketInfo();

                    if (!RawPacketParser.TryParseEthernet(ref ethernetPacketInfo, data,
                            out var ethernetHeaderLength))
                        return; // Invalid Ethernet packet

                    if (!ethernetPacketInfo.IsIPv4 && !ethernetPacketInfo.IsIPv6)
                        return; // We consider only IP packet 

                    if (source.Mac is { } mac) {
                        if (ethernetPacketInfo.DestinationMac != mac &&
                            ethernetPacketInfo.SourceMac != mac)
                            return; // Unknown mac address or not Ethernet II, we don't handle this now

                        sentPacket = ethernetPacketInfo.SourceMac == mac;
                    }

                    data = data.Slice(ethernetHeaderLength);

                    break;
                }

                case LinkLayers.Null:
                case LinkLayers.Loop: {
                    if (data.Length < 4)
                        return;

                    data = data.Slice(4);

                    break;
                }

                case LinkLayers.Raw:
                case LinkLayers.RawLegacy:
                    break;

                default:
                    return;
            }

            var ipPacketInfo = new IpPacketInfo();

            if (!RawPacketParser.TryParseIp(ref ipPacketInfo, data))
                return; // Invalid IP packet here 

            if (!ipPacketInfo.IsTcp)
                return; // TCP packet only, update need here for H3

            data = data.Slice(ipPacketInfo.HeaderLength);

            var tcpPacketInfo = new TcpPacketInfo();

            if (!RawPacketParser.TryParseTcp(ref tcpPacketInfo, data))
                return; // Not a valid TCP packet

            IPAddress remoteAddress;
            int remotePort;
            int localPort;

            if (sentPacket == null) {
                // No link layer hint (loopback or raw IP): the direction is
                // inferred from which end is a registered authority

                var destinationKey =
                    PacketKeyBuilder.GetAuthorityKey(ipPacketInfo.DestinationIp, tcpPacketInfo.DestinationPort);

                if (_knownAuthorities.ContainsKey(destinationKey))
                    sentPacket = true;
                else {
                    var sourceKey =
                        PacketKeyBuilder.GetAuthorityKey(ipPacketInfo.SourceIp, tcpPacketInfo.SourcePort);

                    if (!_knownAuthorities.ContainsKey(sourceKey))
                        return; // No one registered for this 

                    sentPacket = false;
                }
            }

            remoteAddress = sentPacket.Value ? ipPacketInfo.DestinationIp : ipPacketInfo.SourceIp;
            remotePort = sentPacket.Value ? tcpPacketInfo.DestinationPort : tcpPacketInfo.SourcePort;
            localPort = sentPacket.Value ? tcpPacketInfo.SourcePort : tcpPacketInfo.DestinationPort;

            var authorityKey = PacketKeyBuilder.GetAuthorityKey(remoteAddress, remotePort);

            // Check if included authority 
            if (!_knownAuthorities.ContainsKey(authorityKey))
                return; // No one registered for this 

            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddress);
            var writer = _packetQueue.GetOrAdd(connectionKey);

            if (writer.Faulted)
                return;

            try {
                writer.Write(ref capture);
            }
            catch {
                // We ignore any write error here to not break the capture thread
            }
        }

        private sealed class CaptureSource : IDisposable
        {
            private readonly DirectCaptureContext _context;
            private readonly PacketArrivalEventHandler _handler;
            private bool _started;

            public CaptureSource(PcapDevice device, long? mac, DirectCaptureContext context)
            {
                Device = device;
                Mac = mac;
                _context = context;
                _handler = OnPacketArrival;
            }

            public PcapDevice Device { get; }

            public long? Mac { get; }

            public void Start()
            {
                Device.OnPacketArrival += _handler;
                Device.Open(DeviceModes.MaxResponsiveness);
                Device.Filter = "tcp"; // TODO H3 : add udp
                Device.StartCapture();
                _started = true;
            }

            public void Stop()
            {
                if (!_started)
                    return;

                _started = false;

                Device.StopCapture();
                Device.OnPacketArrival -= _handler;
            }

            public void Dispose()
            {
                Stop();
                Device.Dispose();
            }

            private void OnPacketArrival(object sender, PacketCapture capture)
            {
                _context.OnPacketArrival(this, ref capture);
            }
        }
    }
}
