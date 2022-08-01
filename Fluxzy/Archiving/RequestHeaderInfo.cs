using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json.Serialization;
using Fluxzy.Clients;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy
{
    /// <summary>
    /// This data structure is used for serialization only 
    /// </summary>
    public class RequestHeaderInfo
    {
        [JsonConstructor]
        public RequestHeaderInfo()
        {

        }

        public RequestHeaderInfo(RequestHeader originalHeader)
        {
            Method = originalHeader.Method;
            Scheme = originalHeader.Scheme;
            Path = originalHeader.Path;
            Authority = originalHeader.Authority;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s)); 
        }

        public ReadOnlyMemory<char> Method { get; set; }

        public ReadOnlyMemory<char> Scheme { get; set; }

        public ReadOnlyMemory<char> Path { get; set; }

        public ReadOnlyMemory<char> Authority { get; set; }

        public IEnumerable<HeaderFieldInfo> Headers { get; set; }

        public string GetFullUrl()
        {
            return $"{Scheme}://{Authority}{Path}";
        }
    }

    public class ResponseHeaderInfo
    {
        [JsonConstructor]
        public ResponseHeaderInfo()
        {

        }

        public ResponseHeaderInfo(ResponseHeader originalHeader)
        {
            StatusCode = originalHeader.StatusCode;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s)); 
        }

        public int StatusCode { get; set;  } 

        public IEnumerable<HeaderFieldInfo> Headers { get; set; }

    }

    public class HeaderFieldInfo
    {
        [JsonConstructor]
        public HeaderFieldInfo()
        {

        }

        public HeaderFieldInfo(HeaderField original)
        {
            Name = original.Name;
            Value = original.Value;
            Forwarded = !Http11Constants.IsNonForwardableHeader(original.Name); 
        }

        public ReadOnlyMemory<char> Name { get; set; } 

        public ReadOnlyMemory<char> Value { get; set; } 
        
        public bool Forwarded { get; set; }

        public static implicit operator HeaderFieldInfo(HeaderField d) => new HeaderFieldInfo(d);
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