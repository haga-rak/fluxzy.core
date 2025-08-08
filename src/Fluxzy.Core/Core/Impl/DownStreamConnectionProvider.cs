// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Fluxzy.Core
{
    internal class DownStreamConnectionProvider : IDownStreamConnectionProvider
    {
        private readonly List<TcpListener> _listeners;

        private readonly Channel<IAsyncResult> _pendingClientConnections =
            Channel.CreateUnbounded<IAsyncResult>(new UnboundedChannelOptions {
                SingleWriter = true,
                SingleReader = true
            });

        private readonly CancellationTokenSource _tokenSource = new();

        private CancellationToken _token;
        private bool _disposed;

        public DownStreamConnectionProvider(IEnumerable<ProxyBindPoint> boundPoints)
        {
            _listeners = boundPoints.Select(b => new TcpListener(b.EndPoint)).ToList();

            _token = _tokenSource.Token;
        }

        public async ValueTask<TcpClient?> GetNextPendingConnection()
        {
            if (!_listeners.Any())
                return null;

            try {
                var available = await _pendingClientConnections.Reader.WaitToReadAsync(_token); 

                if (!available)
                    return null;

                while (_pendingClientConnections.Reader.TryRead(out var asyncState)) {

                    var listener = (TcpListener?)asyncState.AsyncState;

                    if (listener != null)
                    {
                        var tcpClient = listener.EndAcceptTcpClient(asyncState);

                        tcpClient.NoDelay = true;

                        if (FluxzySharedSetting.DownStreamProviderReceiveTimeoutMilliseconds >= 0)
                        {
                            tcpClient.ReceiveTimeout = FluxzySharedSetting.DownStreamProviderReceiveTimeoutMilliseconds;
                        }

                        return tcpClient;
                    }
                }
            }
            catch (Exception) {
                // Listener Stop was probably called 
            }

            return null;
        }

        public IReadOnlyCollection<IPEndPoint> ListenEndpoints { get; private set; } = Array.Empty<IPEndPoint>();

        public IReadOnlyCollection<IPEndPoint> Init(CancellationToken token)
        {
            _token = token;

            var boundEndPoints = new List<IPEndPoint>();

            foreach (var listener in _listeners) {
                try {
                    listener.Start();

                    boundEndPoints.Add((IPEndPoint) listener.LocalEndpoint);

                    var listenerCopy = listener;

                    new Thread(a => HandleAcceptConnection((TcpListener) a!)) {
                        IsBackground = true,
                        Priority = ThreadPriority.Normal
                    }.Start(listenerCopy);
                }
                catch (SocketException sex) {
                    throw new Exception(
                        $"Cannot bind to this socket: " +
                        $"{((IPEndPoint) listener.LocalEndpoint).Address}:" +
                        $"{((IPEndPoint) listener.LocalEndpoint).Port}  \r\n" +
                        $"Another instance running ?\r\n" +
                        $"- \r\n{sex.Message}");
                }
            }

            if (!boundEndPoints.Any())
                throw new InvalidOperationException("No listen endpoints was provided");

            ListenEndpoints = boundEndPoints;

            return ListenEndpoints;
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            foreach (var listener in _listeners) {
                try {
                    listener.Stop();
                }
                catch (Exception) {
                    // Ignore errors
                }
            }

            _pendingClientConnections.Writer.TryComplete();
            _tokenSource.Cancel();
        }

        private void HandleAcceptConnection(TcpListener listener)
        {
            try {
                listener.BeginAcceptTcpClient(Callback, listener);
            }
            catch (Exception) {
                // Connection closed 
            }
        }

        private void Callback(IAsyncResult ar)
        {
            try {
                var listener = (TcpListener?) ar.AsyncState;

                if (!_pendingClientConnections.Writer.TryWrite(ar) || _disposed)
                    return; 
                
                listener?.BeginAcceptTcpClient(Callback, listener);
            }
            catch (Exception) {
                // ignored
            }
        }
    }
}
