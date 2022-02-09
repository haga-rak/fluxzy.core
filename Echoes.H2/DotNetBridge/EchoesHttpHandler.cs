﻿// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Encoding.Utils;

namespace Echoes.H2.DotNetBridge
{
    public class EchoesHttp2Handler : HttpMessageHandler
    {
        private readonly H2StreamSetting _streamSetting;
        private readonly IDictionary<string, H2ConnectionPool>
            _activeConnections = new Dictionary<string, H2ConnectionPool>();

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        private readonly Http11Parser _parser = new Http11Parser(8192, new ArrayPoolMemoryProvider<char>()); 
        
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

            var exchange = new Exchange(new Authority(request.RequestUri.Host, request.RequestUri.Port,
                true), request.ToHttp11String().AsMemory(), _parser)
            {
                Request = { Body = await request.Content.ReadAsStreamAsync() }
            }; 

            await _activeConnections[request.RequestUri.Authority].Send(exchange,
                cancellationToken).ConfigureAwait(false); 

            return new EchoesHttpResponseMessage(exchange);
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