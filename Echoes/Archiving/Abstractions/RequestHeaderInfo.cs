using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Echoes.H2.Encoder;
using Echoes.H2.Encoder.Utils;

namespace Echoes.Archiving.Abstractions
{
    /// <summary>
    /// This data structure is used for serialization only 
    /// </summary>
    public readonly struct RequestHeaderInfo
    {
        public RequestHeaderInfo(RequestHeader originalHeader)
        {
            Method = originalHeader.Method;
            Path = originalHeader.Path;
            Authority = originalHeader.Authority;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s)); 
        }

        public ReadOnlyMemory<char> Method { get;  }

        public ReadOnlyMemory<char> Path { get;  }

        public ReadOnlyMemory<char> Authority { get;  }

        public IEnumerable<HeaderFieldInfo> Headers { get;  }
    }

    public readonly struct ResponseHeaderInfo
    {
        public ResponseHeaderInfo(ResponseHeader originalHeader)
        {
            StatusCode = originalHeader.StatusCode;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s)); 
        }

        public int StatusCode { get; } = -1;

        public IEnumerable<HeaderFieldInfo> Headers { get;  }

    }

    public readonly struct HeaderFieldInfo
    {
        public HeaderFieldInfo(HeaderField original)
        {
            Name = original.Name;
            Value = original.Value;
            Forwarded = !Http11Constants.IsNonForwardableHeader(original.Name); 
        }

        public ReadOnlyMemory<char> Name { get;  } 

        public ReadOnlyMemory<char> Value { get;  } 
        
        public bool Forwarded { get;  }
    }

    public class SslInfo
    {
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