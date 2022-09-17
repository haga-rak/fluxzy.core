// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Sockets;
using Fluxzy.Core;
using Fluxzy.Misc;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Fluxzy.Interop.Pcap
{
    public class CapturedTcpConnection : ITcpConnection
    {
        private readonly CaptureFileWriterDevice _captureDeviceWriter;
        private readonly PcapDevice _captureDevice;
        private readonly TcpClient _innerTcpClient;
        private readonly IPEndPoint _localEndPoint;

        private DisposeEventNotifierStream?  _stream; 

        private PcapHeader _pcapHeader = new(0, 0, 0, 0);
        private bool _halted = false; 

        public CapturedTcpConnection(string outTraceFileName)
        {
            _localEndPoint = IpUtility.GetFreeEndpoint();
            _innerTcpClient = new TcpClient(_localEndPoint);

            _captureDeviceWriter = new CaptureFileWriterDevice(outTraceFileName, FileMode.Create);
            _captureDeviceWriter.Open();
            
            _captureDevice = CaptureDeviceList.Instance.OfType<PcapDevice>()
                .First(d => d.Interface.Addresses.Any(
                a => Equals(a.Addr.ipAddress, _localEndPoint.Address)));

            _captureDevice.Open(DeviceModes.MaxResponsiveness | DeviceModes.NoCaptureLocal );

            _captureDevice.Filter = $"port {_localEndPoint.Port} and host {_localEndPoint.Address}";

            _captureDevice.OnPacketArrival += OnCaptureDeviceOnOnPacketArrival;

            _captureDevice.StartCapture();
            // Determine port 
        }

        private void OnCaptureDeviceOnOnPacketArrival(object sender, PacketCapture capture)
        {
            _pcapHeader.Timeval = capture.Header.Timeval;

            _pcapHeader.CaptureLength = (uint) capture.Data.Length;
            _pcapHeader.PacketLength = (uint) capture.Data.Length;

            var ethernet = (EthernetPacket) capture.GetPacket().GetPacket();
            var ipPacket = (IPPacket) ethernet.PayloadPacket;

          //  ipPacket.

            _captureDeviceWriter.Write(capture.Data, ref _pcapHeader);
        }

        private void HaltCapture()
        {
            if (_halted)
                return;

            _halted = true;

            _captureDevice.OnPacketArrival -= OnCaptureDeviceOnOnPacketArrival;

            _captureDeviceWriter.StopCapture();

            if (_captureDevice.Opened)
                _captureDevice.StopCapture();

        }


        public Task ConnectAsync(string remoteHost, int port)
        {
            return _innerTcpClient.ConnectAsync(remoteHost, port); 
        }

        public async Task ConnectAsync(IPAddress address, int port)
        {
            if (_stream != null)
                throw new InvalidOperationException("A previous connect attempt was already made");
            try
            {
                await _innerTcpClient.ConnectAsync(address, port);
            }
            catch (NotSupportedException)
            {
                throw;
            }

            _stream = new DisposeEventNotifierStream(_innerTcpClient.GetStream());
            _stream.OnStreamDisposed += (_, _) => HaltCapture();
        }

        public Stream GetStream()
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected yet");

            return _stream;
        }

        public IPEndPoint  LocalEndPoint => _localEndPoint;

        public void Dispose()
        {
            _innerTcpClient.Dispose();

            HaltCapture();

            _captureDevice.Dispose();
            _captureDeviceWriter.Dispose();
        }
    }
}