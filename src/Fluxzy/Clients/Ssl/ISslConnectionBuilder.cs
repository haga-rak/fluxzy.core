// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Fluxzy.Clients.Ssl
{
    public interface ISslConnectionBuilder
    {
        Task<SslConnection> AuthenticateAsClient(
            Stream innerStream,
            SslClientAuthenticationOptions request,
            Action<string> onKeyReceived,
            CancellationToken token);
    }

    public class SslConnection : IDisposable
    {
        public SslConnection(Stream stream, SslInfo sslInfo, SslApplicationProtocol applicationProtocol)
        {
            Stream = stream;
            SslInfo = sslInfo;
            ApplicationProtocol = applicationProtocol;
        }

        public Stream Stream { get; }

        public SslInfo SslInfo { get; }

        public SslApplicationProtocol ApplicationProtocol { get; }

        public string? NssKey { get; set; }

        public void Dispose()
        {
            Stream.Dispose();
        }
    }
}
