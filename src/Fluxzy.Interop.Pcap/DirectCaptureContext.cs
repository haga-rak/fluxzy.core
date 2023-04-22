// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Concurrent;
using System.Net;
using Fluxzy.Interop.Pcap.Reading;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    public class DirectCaptureContext : ICaptureContext
    {
        private readonly PcapDevice _captureDevice;

        private readonly ConcurrentDictionary<long, object?> _knownAuthorities = new();
        private readonly long _physicalAddressLong;
        private bool _disposed;

        private bool _halted;
        private SyncWriterQueue? _packetQueue;

        public DirectCaptureContext(IPAddress? localAddress = null)
        {
            var localAddress1 = localAddress ?? IpUtility.GetDefaultRouteV4Address();

            // TODO: use a better heuristic to select the capture device
            // TODO: demand the interface name as parameter

            _captureDevice = CaptureDeviceList.Instance.OfType<PcapDevice>()
                                              .Where(l => !l.IsLoopback())
                                              .OrderByDescending(l => l.IsUp())
                                              .ThenByDescending(l => l.IsRunning())
                                              .ThenByDescending(l => l.IsConnected())
                                              .ThenByDescending(d => d.Interface.Addresses.Any(
                                                  a => Equals(a.Addr.ipAddress, localAddress1)))
                                              .ThenByDescending(d =>
                                                  d.Interface.GatewayAddresses.Any(g => !g.IsIPv6LinkLocal))
                                              .First();

            _physicalAddressLong = NetUtility.MacToLong(_captureDevice.MacAddress.GetAddressBytes());

            //Start();
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

            var writer = _packetQueue.GetOrAdd(connectionKey);

            writer.Register(outFileName); // There is a change that the writer is already registered

            return writer.Key;
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

            _packetQueue.TryRemove(subscription, out _);

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

                _captureDevice.OnPacketArrival += OnCaptureDeviceOnPacketArrival;
                _captureDevice.Open(DeviceModes.MaxResponsiveness);
                _captureDevice.Filter = "tcp"; // TODO H3 : add udp
                _captureDevice.StartCapture();
            }

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            lock (this) {
                if (_disposed)
                    return ValueTask.CompletedTask;

                _disposed = true;

                Stop();

                _captureDevice.Dispose();
                _packetQueue?.Dispose();
            }

            return ValueTask.CompletedTask;
        }

        private void Stop()
        {
            if (_halted)
                return;

            _halted = true;

            _captureDevice.StopCapture();
            _captureDevice.OnPacketArrival -= OnCaptureDeviceOnPacketArrival;
        }

        private void OnCaptureDeviceOnPacketArrival(object sender, PacketCapture capture)
        {
            if (_packetQueue == null)
                return;

            var ethernetPacketInfo = new EthernetPacketInfo();

            var indexedPackedData = capture.Data;

            if (!RawPacketParser.TryParseEthernet(ref ethernetPacketInfo, indexedPackedData,
                    out var ethernetHeaderLength))
                return; // Invalid Ethernet packet

            if (ethernetPacketInfo.DestinationMac != _physicalAddressLong &&
                ethernetPacketInfo.SourceMac != _physicalAddressLong)
                return; // Unknown mac address or not Ethernet II, we don't handle this now

            if (!ethernetPacketInfo.IsIPv4 && !ethernetPacketInfo.IsIPv6)
                return; // We consider only IP packet 

            indexedPackedData = indexedPackedData.Slice(ethernetHeaderLength);

            var ipPacketInfo = new IpPacketInfo();

            if (!RawPacketParser.TryParseIp(ref ipPacketInfo, indexedPackedData))
                return; // Invalid IP packet here 

            if (!ipPacketInfo.IsTcp)
                return; // TCP packet only, update need here for H3

            indexedPackedData = indexedPackedData.Slice(ipPacketInfo.HeaderLength);

            var tcpPacketInfo = new TcpPacketInfo();

            if (!RawPacketParser.TryParseTcp(ref tcpPacketInfo, indexedPackedData))
                return; // Not a valid TCP packet

            // This should be true 

            var sentPacket = ethernetPacketInfo.SourceMac == _physicalAddressLong;

            var remoteAddress = sentPacket ? ipPacketInfo.DestinationIp : ipPacketInfo.SourceIp;
            var remotePort = sentPacket ? tcpPacketInfo.DestinationPort : tcpPacketInfo.SourcePort;
            var localPort = !sentPacket ? tcpPacketInfo.DestinationPort : tcpPacketInfo.SourcePort;

            var authorityKey = PacketKeyBuilder.GetAuthorityKey(remoteAddress, remotePort);

            // Check if included authority 
            if (!_knownAuthorities.ContainsKey(authorityKey))
                return; // No one registered for this 

            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddress);
            var writer = _packetQueue.GetOrAdd(connectionKey);

            if (writer.Faulted)
                return;

            try {
                writer.Write(capture);
            }
            catch {
                // We ignore any write error here to not break the capture thread
            }
        }
    }
}
