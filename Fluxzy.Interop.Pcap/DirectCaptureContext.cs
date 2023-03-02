// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Concurrent;
using System.Net;
using Fluxzy.Interop.Pcap.FastParsing;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    public class DirectCaptureContext : IAsyncDisposable, ICaptureContext
    {
        private readonly PcapDevice _captureDevice;

        private bool _halted;
        private SyncWriterQueue? _packetQueue;
        private bool _disposed;
        private readonly long _physicalAddressLong;
        
        private readonly ConcurrentDictionary<long, object?> _knownAuthorities = new();

        public DirectCaptureContext(IPAddress?  localAddress = null)
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
                                              .ThenByDescending(d => d.Interface.GatewayAddresses.Any(g => !g.IsIPv6LinkLocal))
                                              .First();
            
            _physicalAddressLong = NetUtility.MacToLong(_captureDevice.MacAddress.GetAddressBytes());
            
            //Start();
        }

        public void Include(IPAddress remoteAddress, int remotePort)
        {
            _knownAuthorities.TryAdd(PacketKeyBuilder.GetAuthorityKey(remoteAddress, remotePort), null);
        }

        public long Subscribe(string outFileName,
            IPAddress remoteAddress, int remotePort, int localPort)
        {
            if (_packetQueue == null)
                throw new InvalidOperationException("Not started yet");

            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddress);
            
            var writer = _packetQueue.GetOrAdd(connectionKey);

            writer.Register(outFileName); // There is a change that the writer is already registered

            return writer.Key;
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

        public Task Start()
        {
            _captureDevice.OnPacketArrival += OnCaptureDeviceOnPacketArrival;
            _captureDevice.Open(DeviceModes.MaxResponsiveness);
            //_captureDevice.Open();
            _packetQueue = new SyncWriterQueue();
            _captureDevice.Filter = $"tcp"; // TODO H3 : add udp
            _captureDevice.StartCapture();

            return Task.CompletedTask;
        }
        

        public void Stop()
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

            if (!RawPacketParser.TryParseEthernet(ref ethernetPacketInfo, indexedPackedData, out var ethernetHeaderLength))
                return;  // Invalid Ethernet packet
            
            if (ethernetPacketInfo.DestinationMac != _physicalAddressLong &&
                ethernetPacketInfo.SourceMac != _physicalAddressLong)
                return; // Unknown mac address or not Ethernet II, we don't handle this now

            if (!ethernetPacketInfo.IsIPv4 && !ethernetPacketInfo.IsIPv6) 
                return; // We consider only IP packet 

            indexedPackedData = indexedPackedData.Slice(ethernetHeaderLength);

            var ipPacketInfo = new IpPacketInfo();

            if (!RawPacketParser.TryParseIp(ref ipPacketInfo, indexedPackedData))
                return;  // Invalid IP packet here 

            if (!ipPacketInfo.IsTcp)
                return; // TCP packet only, update need here for H3
            
            indexedPackedData = indexedPackedData.Slice(ipPacketInfo.HeaderLength);

            var tcpPacketInfo = new TcpPacketInfo(); 

            if (!RawPacketParser.TryParseTcp(ref tcpPacketInfo, indexedPackedData))
                return;  // Not a valid TCP packet

            // This should be true 
            
            var consumed = indexedPackedData.Length == ipPacketInfo.PayloadLength;
            var sentPacket = ethernetPacketInfo.SourceMac == _physicalAddressLong;
            
            var remoteAddr = sentPacket ? ipPacketInfo.DestinationIp : ipPacketInfo.SourceIp;
            var remotePort = sentPacket ? tcpPacketInfo.DestinationPort : tcpPacketInfo.SourcePort;
            var localPort = !sentPacket ? tcpPacketInfo.DestinationPort : tcpPacketInfo.SourcePort;

            var authorityKey = PacketKeyBuilder.GetAuthorityKey(remoteAddr, remotePort);

            // Check if included authority 
            if (!_knownAuthorities.ContainsKey(authorityKey))
                return; // No one registered for this 

            var connectionKey = PacketKeyBuilder.GetConnectionKey(localPort, remotePort, remoteAddr);
            var writer = _packetQueue.GetOrAdd(connectionKey);

            if (writer.Faulted)
                return;

            try {
                writer.Write(capture.Data, capture.Header.Timeval);
            }
            catch {
                // We ignore any write error here to not break the capture thread
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            Stop();

            _captureDevice.Dispose();
            _packetQueue.Dispose();
        }
    }
}