// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.Ssl.SChannel
{
    public class SChannelConnectionBuilder : ISslConnectionBuilder
    {
        public async Task<SslConnection> AuthenticateAsClient(
            Stream innerStream,
            SslClientAuthenticationOptions request, Action<string> onKeyReceived, CancellationToken token)
        {
            var sslStream = new SslStream(innerStream, false);

            await sslStream.AuthenticateAsClientAsync(request, token);

            var sslInfo = new SslInfo(sslStream);

            return new SslConnection(sslStream, sslInfo, sslStream.NegotiatedApplicationProtocol);
        }
    }
}
