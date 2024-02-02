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

        //private readonly Channel<IAsyncResult> _pendingClientConnections =
        //    Channel.CreateUnbounded<IAsyncResult>(new UnboundedChannelOptions {
        //        SingleWriter = true,
        //        SingleReader = true
        //    });

        private readonly Channel<TcpClient> _pendingClientConnections =
            Channel.CreateUnbounded<TcpClient>(new UnboundedChannelOptions {
                SingleWriter = true,
                SingleReader = true
            });

        private readonly CancellationTokenSource _tokenSource = new();

        private CancellationToken _token;

        public DownStreamConnectionProvider(IEnumerable<ProxyBindPoint> boundPoints)
        {
            _listeners = boundPoints.Select(b => new TcpListener(b.EndPoint)).ToList();

            _token = _tokenSource.Token;
        }

        public bool TryGetNextPendingConnection(out TcpClient client)
        {
            if (_pendingClientConnections.Reader.TryRead(out var tcpClient)) {
                client = tcpClient;
                return true;
            }

            client = null!;
            return false;
        }

        public async ValueTask<TcpClient?> GetNextPendingConnection()
        {
            if (!_listeners.Any())
                return null;

            try {

                if (_pendingClientConnections.Reader.TryRead(out var client)) {
                    return client;
                }

                var tcpClient = await
                    _pendingClientConnections.Reader.ReadAsync(_token);

                tcpClient.NoDelay = true;

                if (FluxzySharedSetting.DownStreamProviderReceiveTimeoutMilliseconds >= 0)
                {
                    tcpClient.ReceiveTimeout = FluxzySharedSetting.DownStreamProviderReceiveTimeoutMilliseconds;
                }

                return tcpClient;

                //var listener = (TcpListener?) asyncState.AsyncState;

                //if (listener != null) {

                //    var tcpClient = listener.EndAcceptTcpClient(asyncState);

                //}
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
                    listener.Start(1000);

                    boundEndPoints.Add((IPEndPoint) listener.LocalEndpoint);

                    _ = Task.Run(() => { LoopAccept(token, listener); }, token);

                    //var listenerCopy = listener;




                    //new Thread(a => HandleAcceptConnection((TcpListener) a!)) {
                    //    IsBackground = true,
                    //    Priority = ThreadPriority.Normal
                    //}.Start(listenerCopy);
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

        private void LoopAccept(CancellationToken token, TcpListener listener)
        {
            try
            {
                while (!token.IsCancellationRequested) {
                    var client = listener.AcceptTcpClient();
                    _pendingClientConnections.Writer.TryWrite(client);
                }
            }
            catch (Exception) {
                // ignored
            }
        }

        public void Dispose()
        {
            foreach (var listener in _listeners) {
                try {
                    listener.Stop();
                }
                catch (Exception) {
                    // Ignore errors
                }
            }

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

                //_pendingClientConnections.Writer.TryWrite(ar);
                //listener?.BeginAcceptTcpClient(Callback, listener); 
            }
            catch (Exception) {
                // ignored
            }
        }
    }
}
