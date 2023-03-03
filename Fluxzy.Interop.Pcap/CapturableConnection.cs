// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Sockets;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Interop.Pcap
{
    public class CapturableTcpConnection :  ITcpConnection
    {
        private readonly ProxyScope _proxyScope;
        private readonly string _outTraceFileName;
        private readonly TcpClient _innerTcpClient;
        private DisposeEventNotifierStream?  _stream;

        private bool _disposed = false;
        private IPEndPoint?  _localEndPoint;
        private long  _subscription;
        private IPAddress _remoteAddress;

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

            context?.Include(remoteAddress, remotePort);

            await _innerTcpClient.ConnectAsync(remoteAddress, remotePort);

            _remoteAddress = remoteAddress;
            _localEndPoint = (IPEndPoint) _innerTcpClient.Client.LocalEndPoint!;


            _subscription = context?.Subscribe(_outTraceFileName, remoteAddress, remotePort, _localEndPoint.Port) ?? 0;

            _stream = new DisposeEventNotifierStream(_innerTcpClient.GetStream());
            
            _stream.OnStreamDisposed += async (_, _) =>
            {
                // TODO when stream disposed 
                await DisposeAsync();
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
                
                _proxyScope.CaptureContext.StoreKey(nssKey, _remoteAddress, remoteEndPoint.Port,
                    _localEndPoint.Port);
            }

        }

        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true; 

            _innerTcpClient.Dispose();

            if (_subscription != 0 )
            {
                var context = _proxyScope.CaptureContext;

                if (context != null)
                    await context.Unsubscribe(_subscription);

                // We should wait few instant before disposing the writer 
                // to ensure that all packets are written to the file
            }
        }
    }

    //public static class ProxyScopeExtensions
    //{
    //    public static async Task<ICaptureContext?> GetCaptureContext(this ProxyScope scope)
    //    {
    //        var captureHost = await scope.GetOrCreateHostedCaptureHost();

    //        if (captureHost == null)
    //            return null; 

    //        if (captureHost.Context is ICaptureContext captureContext)
    //            return captureContext;

    //        return null; 
    //    }
    //}
}