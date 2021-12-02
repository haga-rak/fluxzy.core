// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Echoes.H2.DotNetBridge
{
    public class EchoesHttp2Handler : HttpMessageHandler
    {
        private readonly H2StreamSetting _streamSetting;
        private Dictionary<string, H2ClientConnection> _activeConnection = new Dictionary<string, H2ClientConnection>();
        
        public EchoesHttp2Handler(H2StreamSetting streamSetting = null)
        {
            _streamSetting = streamSetting ?? new H2StreamSetting();
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {

            if (!_activeConnection.TryGetValue(request.RequestUri.Authority, out var connection))
            {
                connection = await H2ConnectionBuilder.Create(
                    request.RequestUri.Host,
                    request.RequestUri.Port, _streamSetting, cancellationToken).ConfigureAwait(false);

                _activeConnection[request.RequestUri.Authority] = connection; 
            }

            

            var response = await connection.Send(request.ToHttp11String().AsMemory(),
                request.Content != null ? await request.Content.ReadAsStreamAsync() : null,
                request.Content?.Headers.ContentLength ?? -1,
                cancellationToken).ConfigureAwait(false); 

            return new EchoesHttpResponseMessage(response);
        }


        protected override void Dispose(bool disposing)
        {
            
            base.Dispose(disposing);

            foreach (var connection in _activeConnection.Values)
            {
                connection.Dispose();
            }
        }

    }
}