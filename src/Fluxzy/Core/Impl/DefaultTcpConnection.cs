// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    public class DefaultTcpConnection : ITcpConnection
    {
        private readonly TcpClient _client;

        public DefaultTcpConnection()
        {
            _client = new TcpClient();
            _client.NoDelay = true; 
        }
        
        public Task<IPEndPoint> ConnectAsync(IPAddress address, int port)
        {
            return _client.ConnectAsync(address, port)
                          .ContinueWith(t =>
                          {
                              if (t.Exception != null && t.Exception.InnerExceptions.Any())
                                  throw t.Exception.InnerExceptions.First();

                              return (IPEndPoint)_client.Client.LocalEndPoint;
                          });
        }

        public Stream GetStream()
        {
            var resultStream =
                new DisposeEventNotifierStream(_client.GetStream());

            resultStream.OnStreamDisposed += ResultStreamOnOnStreamDisposed;

            return resultStream;
        }

        public void OnKeyReceived(string nssKey)
        {
            // Ignore
        }

        public async ValueTask DisposeAsync()
        {
            _client?.Dispose();
            await Task.CompletedTask;
        }

        private async Task ResultStreamOnOnStreamDisposed(object sender, StreamDisposeEventArgs args)
        {
            var stream = (DisposeEventNotifierStream) sender;
            stream.OnStreamDisposed -= ResultStreamOnOnStreamDisposed;

            await DisposeAsync();
        }
    }
}
