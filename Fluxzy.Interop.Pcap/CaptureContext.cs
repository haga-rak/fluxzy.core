// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.NetworkInformation;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    public class CaptureContext : IDisposable
    {
        private readonly PcapDevice _captureDevice;

        private bool _halted;
        private readonly PhysicalAddress _physicalLocalAddress;
        private readonly PacketQueue _packetQueue = new();
        private bool _disposed;

        public CaptureContext(IPAddress?  localAddress = null)
        {
            var localAddress1 = localAddress ?? IpUtility.GetDefaultRouteV4Address();
            _captureDevice = CaptureDeviceList.Instance.OfType<PcapDevice>().First(d => d.Interface.Addresses.Any(
                a => Equals(a.Addr.ipAddress, localAddress1)));

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

        public void Unsubscribe(IConnectionSubscription subscription)
        {
            _packetQueue.Unsubscribe(subscription);
        } 

        private void Start()
        {
            _captureDevice.Open(DeviceModes.MaxResponsiveness | DeviceModes.NoCaptureLocal);
            _captureDevice.Filter = $"tcp";
            _captureDevice.OnPacketArrival += OnCaptureDeviceOnOnPacketArrival;
            _captureDevice.StartCapture();
        }

        public void Stop()
        {
            if (_halted)
                return;

            _halted = true;

            _captureDevice.OnPacketArrival -= OnCaptureDeviceOnOnPacketArrival;

            if (_captureDevice.Opened)
                _captureDevice.StopCapture();
        }

        private void OnCaptureDeviceOnOnPacketArrival(object sender, PacketCapture capture)
        {
            var rawPacket = capture.GetPacket();
            var ethernetPacket = (EthernetPacket) rawPacket.GetPacket();

            // SE REFERER à la date
            
            _packetQueue.Enqueue(rawPacket, ethernetPacket, _physicalLocalAddress);
        }

        public void Dispose()
        {
            if (_disposed) {
                return;
            }

            _disposed = true; 

            Stop(); 

            _captureDevice.Dispose();
            _packetQueue.Dispose();
        }
    }
    
}