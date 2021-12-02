// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.DotNetBridge
{
    public class EchoesHttp2Handler : HttpMessageHandler
    {
        private readonly H2StreamSetting _streamSetting;
        private ConcurrentDictionary<string, H2ClientConnection>
            _activeConnections = new ConcurrentDictionary<string, H2ClientConnection>();

        private SemaphoreSlim _semaphore = new SemaphoreSlim(1); 
        
        public EchoesHttp2Handler(H2StreamSetting streamSetting = null)
        {
            _streamSetting = streamSetting ?? new H2StreamSetting();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                await _semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (!_activeConnections.TryGetValue(request.RequestUri.Authority, out var connection))
                {
                    connection = await H2ConnectionBuilder.Create(
                        request.RequestUri.Host,
                        request.RequestUri.Port, _streamSetting, cancellationToken).ConfigureAwait(false);

                    _activeConnections[request.RequestUri.Authority] = connection;
                }
            }
            finally
            {
                _semaphore.Release();
            }

            var response = await _activeConnections[request.RequestUri.Authority].Send(request.ToHttp11String().AsMemory(),
                request.Content != null ? await request.Content.ReadAsStreamAsync() : null,
                request.Content?.Headers.ContentLength ?? -1,
                cancellationToken).ConfigureAwait(false); 

            return new EchoesHttpResponseMessage(response);
        }


        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            _semaphore.Dispose();

            foreach (var connection in _activeConnections.Values)
            {
                connection.Dispose();
            }
        }

    }
}