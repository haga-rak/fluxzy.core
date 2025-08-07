// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Tests.Cases
{
    class ConnectionCloseTestServer
    {
        private readonly Func<int, bool> _whenShouldClose;
        private readonly string _responseBodyString;
        private readonly bool _closeTransportFirst;
        private readonly TcpListener _listener = new(IPAddress.Loopback, 0);
        private readonly X509Certificate2 _certificate = new("_Files/Certificates/client-cert.pifix",
            CertificateContext.DefaultPassword);
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public ConnectionCloseTestServer(Func<int, bool> whenShouldClose, 
            string responseBodyString, bool closeTransportFirst)
        {
            _whenShouldClose = whenShouldClose;
            _responseBodyString = responseBodyString;
            _closeTransportFirst = closeTransportFirst;
        }

        public ConnectionCloseTestServerInstance Start()
        {
            _listener.Start();
            var loop = InternalLoop();
            return new ConnectionCloseTestServerInstance(_listener, loop, _cancellationTokenSource);
        }

        private async Task InternalLoop()
        {
            var token = _cancellationTokenSource.Token;

            try {
                while (!token.IsCancellationRequested)
                {
                    var client = await _listener.AcceptTcpClientAsync(token);
                    _ = HandleClient(client, _certificate);
                }
            }
            catch (Exception) {
                // Normal cancellation, do nothing
            }
        }

        private async Task HandleClient(TcpClient client, X509Certificate2 cert)
        {
            using var _ = client;
            await using var networkStream = client.GetStream();
            await using var sslStream = new SslStream(networkStream, false);

            await sslStream.AuthenticateAsServerAsync(cert, clientCertificateRequired: false,
                enabledSslProtocols: SslProtocols.Tls12, checkCertificateRevocation: false);

            var index = 0;

            try {
                while (true)
                {
                    try
                    {
                        var __ = await ReadUntilDoubleCrLfAsync(sslStream);

                        index++;
                        var shouldClose = _whenShouldClose(index);

                        var connectionWord = shouldClose ? "Connection: close\r\n" : "";

                        var response =
                            "HTTP/1.1 200 OK\r\n" +
                            "Host: local.fluxzy.io\r\n" +
                            $"Content-Length: {_responseBodyString.Length}\r\n" + connectionWord +
                            "\r\n" +
                            _responseBodyString;

                        var buffer = Encoding.UTF8.GetBytes(response);
                        await sslStream.WriteAsync(buffer, 0, buffer.Length);

                        if (shouldClose)
                        {
                            break;
                        }

                        await sslStream.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        break;
                    }
                }
            }
            finally {
                if (_closeTransportFirst) {
                    await networkStream.DisposeAsync();
                }
            }
        }

        static async Task<string> ReadUntilDoubleCrLfAsync(Stream stream)
        {
            var ms = new MemoryStream();
            var buffer = new byte[1];
            var matchCount = 0;

            while (matchCount < 4)
            {
                var read = await stream.ReadAsync(buffer, 0, 1);
                if (read == 0) break;

                ms.WriteByte(buffer[0]);

                matchCount = buffer[0] switch
                {
                    (byte)'\r' when matchCount % 2 == 0 => matchCount + 1,
                    (byte)'\n' when matchCount % 2 == 1 => matchCount + 1,
                    _ => 0
                };
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
    
    class ConnectionCloseTestServerInstance : IAsyncDisposable
    {
        private readonly TcpListener _listener;
        private readonly Task _loopTask;
        private readonly CancellationTokenSource _stopSource;

        public ConnectionCloseTestServerInstance(TcpListener listener, Task loopTask, CancellationTokenSource stopSource)
        {
            _listener = listener;
            _loopTask = loopTask;
            _stopSource = stopSource;
            Port = ((IPEndPoint)listener.LocalEndpoint).Port;
        }

        public int Port { get; }

        public async ValueTask DisposeAsync()
        {
            _listener.Stop();
            _listener.Dispose();
            await _stopSource.CancelAsync();
            await _loopTask;
        }
    }
}
