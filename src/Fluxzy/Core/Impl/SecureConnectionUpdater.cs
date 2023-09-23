// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;
using Fluxzy.Misc.Traces;

namespace Fluxzy.Core
{
    public class SecureConnectionUpdater
    {
        private readonly ICertificateProvider _certificateProvider;

        public SecureConnectionUpdater(ICertificateProvider certificateProvider)
        {
            _certificateProvider = certificateProvider;
        }

        private static bool StartWithKeyWord(ReadOnlySpan<byte> buffer)
        {
            Span<char> bufferChar = stackalloc char[4];
            Encoding.ASCII.GetChars(buffer, bufferChar);

            return ((ReadOnlySpan<char>) bufferChar).Equals("GET ", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<SecureConnectionUpdateResult> AuthenticateAsServer(
            Stream stream, string host, CancellationToken token)
        {
            var buffer = new byte[4];
            var originalStream = stream;

            if (stream is NetworkStream networkStream && networkStream.DataAvailable) {
                networkStream.ReadExact(buffer);
            }
            else {
                await stream.ReadExactAsync(buffer, token);
            }

            if (StartWithKeyWord(buffer)) {
                // This is websocket request 

                return new SecureConnectionUpdateResult(false, true,
                    new CombinedReadonlyStream(false, new MemoryStream(buffer), stream),
                    stream);
            }

            stream = new CombinedReadonlyStream(false, new MemoryStream(buffer), stream);

            var secureStream = new SslStream(new RecomposedStream(stream, originalStream), false);

            X509Certificate2 certificate;

            try {
                certificate = _certificateProvider.GetCertificate(host);
            }
            catch (Exception e) {
                if (D.EnableTracing) {
                    D.TraceException(e, "An error occured while getting certificate");
                }
                
                throw;
            }

            try {
                await secureStream
                    .AuthenticateAsServerAsync(certificate, false, SslProtocols.None, false);
            }
            catch (Exception ex) {
                throw new FluxzyException(ex.Message, ex);
            }

            return new SecureConnectionUpdateResult(false, true,
                secureStream,
                secureStream);
        }
    }

    public record SecureConnectionUpdateResult(
        bool IsSsl, bool IsWebSocket,
        Stream InStream, Stream OutStream)
    {
        public bool IsSsl { get; } = IsSsl;

        public bool IsWebSocket { get; } = IsWebSocket;

        public bool IsOnError => !IsSsl && !IsWebSocket;

        public Stream InStream { get; } = InStream;

        public Stream OutStream { get; } = OutStream;
    }
}
