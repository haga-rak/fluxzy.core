using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Core
{
    public class SecureConnectionUpdater
    {
        private readonly ICertificateProvider _certificateProvider;

        public SecureConnectionUpdater(ICertificateProvider certificateProvider)
        {
            _certificateProvider = certificateProvider;
        }

        private bool StartWithKeyWord(ReadOnlySpan<byte> buffer)
        {
            Span<char> bufferChar = stackalloc char[4];
            Encoding.ASCII.GetChars(buffer, bufferChar);
            return ((ReadOnlySpan<char>) bufferChar).Equals("GET ", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<SecureConnectionUpdateResult> AuthenticateAsServer(Stream stream, string host, CancellationToken token)
        {
            var buffer = new byte[4];
            var originalStream = stream;
            
            await stream.ReadExactAsync(buffer, token);
            
            if (StartWithKeyWord(buffer))
            {
                // Probably Web socket request 
                // This is websocket demand 

                return new SecureConnectionUpdateResult(false, true,
                    new CombinedReadonlyStream(false, new MemoryStream(buffer), stream),
                    stream);
            }

            stream = new CombinedReadonlyStream(false,
                new MemoryStream(buffer), stream);

            var secureStream = new SslStream(new RecomposedStream(stream, originalStream), false);

            var certificate = _certificateProvider.GetCertificate(host);

            try
            {
                await secureStream
                    .AuthenticateAsServerAsync(certificate, false, SslProtocols.None, false)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new FluxzyException("Client closed connection while trying to negotiate SSL/TLS settings", ex);
            }


            return new SecureConnectionUpdateResult(false, true,
                secureStream,
                secureStream);
        }
    }

    public record SecureConnectionUpdateResult(bool IsSsl, bool IsWebSocket,
        Stream InStream, Stream OutStream)
    {
        public bool IsSsl { get; } = IsSsl;

        public bool IsWebSocket { get; } = IsWebSocket;

        public bool IsOnError => !IsSsl && !IsWebSocket;

        public Stream InStream { get; } = InStream;

        public Stream OutStream { get; } = OutStream;
    }
}