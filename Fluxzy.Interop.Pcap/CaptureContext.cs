// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    public class CaptureContext : IAsyncDisposable
    {
        private readonly PcapDevice _captureDevice;

        private bool _halted;
        private readonly PhysicalAddress _physicalLocalAddress;
        private PacketQueue _packetQueue;
        private bool _disposed;

        public CaptureContext(IPAddress?  localAddress = null)
        {
            var localAddress1 = localAddress ?? IpUtility.GetDefaultRouteV4Address();

            //targetItem
            _captureDevice = CaptureDeviceList.Instance.OfType<PcapDevice>()
                                              .Where(l => !l.IsLoopback())
                                              .OrderByDescending(l => l.IsUp())
                                              .ThenByDescending(l => l.IsRunning())
                                              .ThenByDescending(l => l.IsConnected())
                                              .ThenByDescending(d => d.Interface.Addresses.Any(
                                                  a => Equals(a.Addr.ipAddress, localAddress1)))
                                              .ThenByDescending(d => d.Interface.GatewayAddresses.Any(g => !g.IsIPv6LinkLocal))
                                              .First();
            
            _physicalLocalAddress = _captureDevice.MacAddress;
            
            Start();
        }

        public void Include(IPAddress remoteAddress, int remotePort)
        {
            _packetQueue.Include(remoteAddress, remotePort);
        }

        public IConnectionSubscription Subscribe(string outFileName, IPAddress remoteAddress, int remotePort, int localPort)
        {
            return _packetQueue.Subscribe(outFileName, remoteAddress, remotePort, localPort);
        } 

        public ValueTask Unsubscribe(IConnectionSubscription subscription)
        {
            return _packetQueue.Unsubscribe(subscription);
        } 

        private void Start()
        {
            _captureDevice.Open();
            _packetQueue = new PacketQueue(_captureDevice.TimestampResolution);
            _captureDevice.Filter = $"tcp";
            _captureDevice.OnPacketArrival += OnCaptureDeviceOnPacketArrival;
            _captureDevice.StartCapture();
            
        }

        public void Stop()
        {
            if (_halted)
                return;

            _halted = true;

            _captureDevice.OnPacketArrival -= OnCaptureDeviceOnPacketArrival;

            _captureDevice.StopCapture();
        }

        private void OnCaptureDeviceOnPacketArrival(object sender, PacketCapture capture)
        {
            var rawPacket = capture.GetPacket();
            
            var ethernetPacket = (EthernetPacket) rawPacket.GetPacket();
            
            _packetQueue.Enqueue(rawPacket, ethernetPacket, _physicalLocalAddress);
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
            await _packetQueue.DisposeAsync();
        }
    }
}