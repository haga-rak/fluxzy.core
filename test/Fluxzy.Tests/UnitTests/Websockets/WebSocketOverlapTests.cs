// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Rules.Actions;
using Xunit;

namespace Fluxzy.Tests.UnitTests.Websockets
{
    public class WebSocketOverlapTests : IDisposable
    {
        private readonly CancellationToken _cancellationToken;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ClientWebSocket _clientWebSocket;
        private readonly FluxzySetting _fluxzySetting;
        private readonly int _pingCount = 2;
        private readonly byte[] _receiveBuffer;

        public WebSocketOverlapTests()
        {
            _cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(TimeoutConstants.Extended));
            _cancellationToken = _cancellationTokenSource.Token;
            _fluxzySetting = FluxzySetting.CreateLocalRandomPort();
            _clientWebSocket = new ClientWebSocket();
            _receiveBuffer = new byte[4096];
        }

        public void Dispose()
        {
            _cancellationTokenSource.Dispose();
            _clientWebSocket.Dispose();
        }

        [Theory]
        [CombinatorialData]
        public async Task Validate_With_Existing_Connection(
            [CombinatorialValues(true, false)] bool forceHttp11,
            [CombinatorialValues(WebSocketMessageType.Binary, WebSocketMessageType.Text)]
            WebSocketMessageType messageType)
        {
            // Arrange
            var wsURl = "wss://echo.websocket.org/";
            var httpUrl = "https://echo.websocket.org/.ws";

            if (forceHttp11) {
                _fluxzySetting.ConfigureRule()
                              .WhenAny().Do(new ForceHttp11Action());
            }

            await using var fluxzyInstance = new Proxy(_fluxzySetting);

            var firstEndPoint = fluxzyInstance.Run().First();
            var proxyConfig = new WebProxy($"http://{firstEndPoint.Address}:{firstEndPoint.Port}");

            _clientWebSocket.Options.Proxy = proxyConfig;

            // Act
            await Warmup_Call(proxyConfig, httpUrl, _cancellationToken);

            await _clientWebSocket.ConnectAsync(new Uri(wsURl), _cancellationToken);
            await _clientWebSocket.ReceiveAsync(_receiveBuffer, _cancellationToken);

            for (var i = 0; i < _pingCount; i++) {
                var originalBytes = Encoding.UTF8.GetBytes($"ping {i}");

                await _clientWebSocket.SendAsync(originalBytes, messageType, true, _cancellationToken);

                var receivedByteCount = await _clientWebSocket.ReceiveAsync(_receiveBuffer, _cancellationToken);

                Assert.True(originalBytes.AsSpan().SequenceEqual(_receiveBuffer.AsSpan(0, receivedByteCount.Count)));
            }
        }

        private static async Task Warmup_Call(WebProxy proxyConfig, string urlPage, CancellationToken token)
        {
            using var httpClient = new HttpClient(new HttpClientHandler {
                Proxy = proxyConfig
            });

            var response = await httpClient.GetAsync(urlPage, token);

            await (await response.Content.ReadAsStreamAsync(token)).DrainAsync();
        }
    }
}
