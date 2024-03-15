// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Net;
using System.Net.Sockets;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core.Pcap
{
    internal class CapturableTcpConnection : ITcpConnection
    {
        private readonly TcpClient _innerTcpClient;
        private readonly string _outTraceFileName;
        private readonly ProxyScope _proxyScope;

        private bool _disposed;
        private IPEndPoint? _localEndPoint;
        private IPAddress? _remoteAddress;
        private DisposeEventNotifierStream? _stream;
        private long _subscription;

        public CapturableTcpConnection(ProxyScope proxyScope, string outTraceFileName)
        {
            _proxyScope = proxyScope;
            _outTraceFileName = outTraceFileName;
            _innerTcpClient = new TcpClient();
        }

        public async Task<IPEndPoint> ConnectAsync(IPAddress remoteAddress, int remotePort)
        {
            if (_stream != null)
                throw new InvalidOperationException("A previous connect attempt was already made");

            var context = _proxyScope.CaptureContext;
            var connectError = false;

            context?.Include(remoteAddress, remotePort);

            try {
                await _innerTcpClient.ConnectAsync(remoteAddress, remotePort).ConfigureAwait(false);

                _remoteAddress = remoteAddress;
                _localEndPoint = (IPEndPoint) _innerTcpClient.Client.LocalEndPoint!;
            }
            catch {
                connectError = true;

                throw;
            }
            finally {
                // We force subscription to this capture context to enable pcapng retrieval for a connection error

                var localPort = ((IPEndPoint?) _innerTcpClient.Client?.LocalEndPoint)?.Port;

                if (localPort != null && localPort > 0) {
                    _subscription = context?.Subscribe(_outTraceFileName, remoteAddress, remotePort, localPort.Value) ??
                                    0;
                }

                if (connectError)
                    await Task.Delay(1000).ConfigureAwait(false);
            }

            _stream = new DisposeEventNotifierStream(_innerTcpClient.GetStream());

            _stream.OnStreamDisposed += async (_, _) => {
                // TODO when stream disposed 
                await DisposeAsync().ConfigureAwait(false);
            };

            return _localEndPoint;
        }

        public Stream GetStream()
        {
            if (_stream == null)
                throw new InvalidOperationException("Not connected yet");

            return _stream;
        }

        public void OnKeyReceived(string nssKey)
        {
            if (_proxyScope.CaptureContext != null && _localEndPoint != null
                                                   && _innerTcpClient.Client.RemoteEndPoint != null) {
                var remoteEndPoint = (IPEndPoint) _innerTcpClient.Client.RemoteEndPoint;

                _proxyScope.CaptureContext.StoreKey(nssKey, _remoteAddress!, remoteEndPoint.Port,
                    _localEndPoint.Port);
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
                return;

            _disposed = true;

            _innerTcpClient.Dispose();

            if (_subscription != 0) {
                var context = _proxyScope.CaptureContext;

                if (context != null)
                    await context.Unsubscribe(_subscription).ConfigureAwait(false);

                // We should wait few instant before disposing the writer 
                // to ensure that all packets are written to the file
            }
        }
    }
}
