// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Sockets;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core.Pcap
{
    internal class CapturableConnectionConnectResult : ITcpConnectionConnectResult
    {
        private readonly ICaptureContext _captureContext;
        private readonly IPEndPoint _remoteEndPoint;
        private readonly IPEndPoint _localEndPoint;

        public CapturableConnectionConnectResult(ICaptureContext captureContext, DisposeEventNotifierStream stream)
        {
            _captureContext = captureContext;
            _remoteEndPoint = stream.RemoteEndPoint;
            _localEndPoint = stream.LocalEndPoint;
            Stream = stream;
        }
        
        public DisposeEventNotifierStream Stream { get; }

        public void ProcessNssKey(string nssKey)
        {
            var remoteEndPoint = _remoteEndPoint;
            _captureContext.StoreKey(nssKey, remoteEndPoint.Address, remoteEndPoint.Port, _localEndPoint.Port);
        }
    }


    internal class CapturableTcpConnection : ITcpConnection
    {
        private readonly TcpClient _innerTcpClient;
        private readonly ICaptureContext _captureContext;
        private readonly string _outTraceFileName;

        private bool _disposed;
        private DisposeEventNotifierStream? _stream;
        private long _subscriptionId;

        public CapturableTcpConnection(ICaptureContext captureContext, string outTraceFileName)
        {
            _captureContext = captureContext;
            _outTraceFileName = outTraceFileName;
            _innerTcpClient = new TcpClient();
        }

        public async Task<ITcpConnectionConnectResult> ConnectAsync(IPAddress remoteAddress, int remotePort)
        {
            if (_stream != null)
                throw new InvalidOperationException("A previous connect attempt was already made");

            var connectError = false;

            _captureContext.Include(remoteAddress, remotePort);

            try {
                await _innerTcpClient.ConnectAsync(remoteAddress, remotePort).ConfigureAwait(false);
            }
            catch {
                connectError = true;
                throw;
            }
            finally {
                // We force subscription to this capture context to enable pcapng retrieval for a connection error

                var localPort = ((IPEndPoint?) _innerTcpClient.Client?.LocalEndPoint)?.Port;

                if (localPort != null && localPort > 0) {
                    _subscriptionId = _captureContext.Subscribe(_outTraceFileName, remoteAddress, remotePort, localPort.Value);
                }

                if (connectError)
                    await Task.Delay(1000).ConfigureAwait(false);
            }

            _stream = new DisposeEventNotifierStream(_innerTcpClient, DisposeAsync);
            return new CapturableConnectionConnectResult(_captureContext, _stream);
        }
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_subscriptionId != 0) {
                await Task.Delay(FluxzySharedSetting.RawCaptureLingerDelayBeforeTearDownMillis).ConfigureAwait(false); 
                await _captureContext.Unsubscribe(_subscriptionId).ConfigureAwait(false);

                // We should wait few instant before disposing the writer 
                // to ensure that all packets are written to the file
            }
        }
    }


}
