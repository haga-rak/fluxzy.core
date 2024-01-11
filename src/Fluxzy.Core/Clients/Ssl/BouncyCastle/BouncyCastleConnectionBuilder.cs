// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Core;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients.Ssl.BouncyCastle
{
    internal class BouncyCastleConnectionBuilder : ISslConnectionBuilder
    {
        private static readonly object SslFileLocker = new();

        public async Task<SslConnection> AuthenticateAsClient(
            Stream innerStream,
            SslConnectionBuilderOptions builderOptions,
            Action<string> onKeyReceived,
            CancellationToken token)
        {
            var crypto = new FluxzyCrypto();

            var tlsAuthentication = 
                new FluxzyTlsAuthentication(crypto, builderOptions.GetBouncyCastleClientCertificateInfo());

            var client = new FluxzyTlsClient(
                builderOptions.TargetHost!,
                SslProtocols.Tls12,
                builderOptions.ApplicationProtocols!.ToArray(), tlsAuthentication, crypto);

            var memoryStream = new MemoryStream();

            var nssWriter = new NssLogWriter(memoryStream) {
                KeyHandler = onKeyReceived
            };

            if (Environment.GetEnvironmentVariable("SSLKEYLOGFILE") is { } str) {
                nssWriter.KeyHandler = nss => {
                    onKeyReceived(nss);

                    lock (SslFileLocker) {
                        try
                        {
                            File.AppendAllText(str, nss);
                        }
                        catch {
                            // ignore ERROR. SSLKEYLOGFILE May be locked by other process
                        }
                    }
                };
            }

            var protocol = new FluxzyClientProtocol(innerStream, nssWriter);

            try
            {
                await protocol.ConnectAsync(client);
            }
            catch (Exception ex)
            {
                throw new ClientErrorException(0, $"Handshake with {builderOptions.TargetHost} has failed", ex.Message);
            }

            var keyInfos =
                Encoding.UTF8.GetString(memoryStream.ToArray());

            // TODO : Keyinfos may be get updated during runtime, must be updated in SslConnection

            var networkStream = innerStream as NetworkStream;

            if (networkStream == null && innerStream is DisposeEventNotifierStream disposeEventNotifierStream) {
                networkStream = (NetworkStream) disposeEventNotifierStream.InnerStream;
            }

            var connection =
                new SslConnection(protocol.Stream, new SslInfo(protocol), protocol.GetApplicationProtocol(),
                    networkStream, innerStream as DisposeEventNotifierStream) {
                    NssKey = keyInfos
                };

            return connection;
        }
    }
}
