// Copyright © 2022 Haga Rakotoharivelo

using System.Net;
using System.Net.Sockets;
using Fluxzy.Core;
using Fluxzy.Misc;

namespace Fluxzy.Interop.Pcap
{
    public class CapturableTcpConnection :  ITcpConnection
    {
        private readonly CaptureContext _captureContext;
        private readonly string _outTraceFileName;
        private readonly TcpClient _innerTcpClient;
        private DisposeEventNotifierStream?  _stream;

        private bool _disposed = false;
        private IPEndPoint?  _localEndPoint;
        private IConnectionSubscription?  _subscription;

        public CapturableTcpConnection(CaptureContext captureContext, string outTraceFileName)
        {
            _captureContext = captureContext;
            _outTraceFileName = outTraceFileName;
            _innerTcpClient = new TcpClient();
            // Determine remotePort 
        }
        
        public async Task<IPEndPoint> ConnectAsync(IPAddress remoteAddress, int remotePort)
        {
            if (_stream != null)
                throw new InvalidOperationException("A previous connect attempt was already made");
            
            _captureContext.Include(remoteAddress, remotePort);

            await _innerTcpClient.ConnectAsync(remoteAddress, remotePort);

            _localEndPoint = (IPEndPoint) _innerTcpClient.Client.LocalEndPoint!;
            _subscription = _captureContext.Subscribe(_outTraceFileName, remoteAddress, remotePort, _localEndPoint.Port);

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
        
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true; 

            _innerTcpClient.Dispose();

            if (_subscription != null )
            {
                var disposable = _subscription;
                _subscription = null;

                _captureContext.Unsubscribe(disposable);

                // Ferme le fichier correspondant s
                await disposable.DisposeAsync();
            }
        }
    }
}