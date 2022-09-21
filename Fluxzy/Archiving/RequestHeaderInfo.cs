using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Fluxzy.Clients;

namespace Fluxzy
{
    /// <summary>
    /// This data structure is used for serialization only 
    /// </summary>
    public class RequestHeaderInfo
    {
        public RequestHeaderInfo(RequestHeader originalHeader)
        {
            Method = originalHeader.Method;
            Scheme = originalHeader.Scheme;
            Path = originalHeader.Path;
            Authority = originalHeader.Authority;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s)); 
        }

        [JsonConstructor]
        public RequestHeaderInfo(ReadOnlyMemory<char> method, ReadOnlyMemory<char> scheme, ReadOnlyMemory<char> path, ReadOnlyMemory<char> authority, IEnumerable<HeaderFieldInfo> headers)
        {
            Method = method;
            Scheme = scheme;
            Path = path;
            Authority = authority;
            Headers = headers;
        }

        public ReadOnlyMemory<char> Method { get; }

        public ReadOnlyMemory<char> Scheme { get; }

        public ReadOnlyMemory<char> Path { get; }

        public ReadOnlyMemory<char> Authority { get; }

        public IEnumerable<HeaderFieldInfo> Headers { get; }

        public string GetFullUrl()
        {
            return $"{Scheme}://{Authority}{Path}";
        }
    }

    public class SslInfo
    {
        [JsonConstructor]
        public SslInfo()
        {

        }

        public SslInfo(SslStream sslStream)
        {
            CipherAlgorithm = sslStream.CipherAlgorithm;
            HashAlgorithm = sslStream.HashAlgorithm;
            KeyExchangeAlgorithm = sslStream.KeyExchangeAlgorithm.ToString();
            NegotiatedApplicationProtocol = sslStream.NegotiatedApplicationProtocol.ToString();
            RemoteCertificateSubject = sslStream.RemoteCertificate?.Subject;
            RemoteCertificateIssuer = sslStream.RemoteCertificate?.Issuer;
            LocalCertificateIssuer = sslStream.LocalCertificate?.Issuer;
            LocalCertificateSubject = sslStream.LocalCertificate?.Subject;
            SslProtocol = sslStream.SslProtocol;
        }

        public SslProtocols SslProtocol { get; set; }

        public string RemoteCertificateIssuer { get;  }

        public string RemoteCertificateSubject { get; }

        public string LocalCertificateSubject { get;  }

        public string LocalCertificateIssuer { get;  }

        public string NegotiatedApplicationProtocol { get;  }

        public string KeyExchangeAlgorithm { get;  }

        public HashAlgorithmType HashAlgorithm { get; }

        public CipherAlgorithmType CipherAlgorithm { get; }
    }
}