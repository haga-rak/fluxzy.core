using System;
using System.IO;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Core.Utils;
using Echoes.Helpers;
using CombinedReadonlyStream = Echoes.IO.CombinedReadonlyStream;

namespace Echoes.Core
{
    public class SecureConnectionUpdater
    {
        private readonly ICertificateProvider _certificateProvider;

        public SecureConnectionUpdater(ICertificateProvider certificateProvider)
        {
            _certificateProvider = certificateProvider;
        }

        private bool StartWithKeyWord(char[] buffer)
        {
            ReadOnlySpan<char> c = buffer.AsSpan();
            return c.Equals("GET ", StringComparison.OrdinalIgnoreCase);
        }

        public async Task<SecureConnectionUpdateResult> AuthenticateAsServer(Stream stream, string host, CancellationToken token)
        {
            var buffer = new byte[4];
            var bufferChar = new char[4];
            var originalStream = stream;


            await stream.ReadExactAsync(buffer, token);

            Encoding.ASCII.GetChars(buffer, bufferChar);

            if (StartWithKeyWord(bufferChar))
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
                throw new EchoesException("Client closed connection while trying to negotiate SSL/TLS settings", ex);
            }


            return new SecureConnectionUpdateResult(false, true,
                secureStream,
                secureStream);
        }
    }

    public class SecureConnectionUpdateResult
    {
        public SecureConnectionUpdateResult(bool isSsl, bool isWebSocket,
            Stream inStream, Stream outStream)
        {
            IsSsl = isSsl;
            IsWebSocket = isWebSocket;
            InStream = inStream;
            OutStream = outStream;
        }

        public bool IsSsl { get; }

        public bool IsWebSocket { get; }

        public bool IsOnError => !IsSsl && !IsWebSocket;

        public Stream InStream { get; }

        public Stream OutStream { get; }
    }
}