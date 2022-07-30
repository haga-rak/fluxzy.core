// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Echoes.Clients.H2.Encoder;
using Echoes.Clients.H2.Encoder.Utils;
using Echoes.Misc;

namespace Echoes.Clients
{
    public class Exchange : IExchange
    {
        private static int ExchangeCounter = 0;

        private readonly TaskCompletionSource<bool> _exchangeCompletionSource = new TaskCompletionSource<bool>();

        public Exchange(
            Authority authority, 
            ReadOnlyMemory<char> requestHeader,
            Stream requestBody,
            ReadOnlyMemory<char> responseHeader,
            Stream responseBody,
            bool isSecure,
            Http11Parser parser, string httpVersion, DateTime receivedFromProxy)
        {
            Id = Interlocked.Increment(ref ExchangeCounter);

            Authority = authority;
            HttpVersion = httpVersion;
            Request = new Request(new RequestHeader(requestHeader, isSecure, parser))
            {
                Body = requestBody ?? StreamUtils.EmptyStream
            };
            Response = new Response()
            {
                Header = new ResponseHeader(responseHeader, isSecure, parser),
                Body = responseBody ?? StreamUtils.EmptyStream
            };

            // TODO : Fill metrics 

            Metrics.ReceivedFromProxy = receivedFromProxy;

            _exchangeCompletionSource.SetResult(false);
        }


        public Exchange(
            Authority authority, 
            RequestHeader requestHeader, Stream bodyStream, string httpVersion, DateTime receivedFromProxy)
        {
            Id = Interlocked.Increment(ref ExchangeCounter); 
            Authority = authority;
            HttpVersion = httpVersion;
            Request = new Request(requestHeader)
            {
                Body = bodyStream
            };
            Metrics.ReceivedFromProxy = receivedFromProxy;
        }

        public Exchange(
            Authority authority, 
            ReadOnlyMemory<char> header, 
            Http11Parser parser, string httpVersion, DateTime receivedFromProxy)
        {
            Id = Interlocked.Increment(ref ExchangeCounter); 
            Authority = authority;
            HttpVersion = httpVersion;
            Request = new Request(new RequestHeader(header, authority.Secure, parser));
            Metrics.ReceivedFromProxy = receivedFromProxy;
        }

        public int Id { get;  }


        public string HttpVersion { get; set; }

        /// <summary>
        /// This tasks indicates the status of the exchange
        /// </summary>
        internal Task<bool> Complete => _exchangeCompletionSource.Task; 

        /// <summary>
        /// The remote authority  
        /// </summary>
        public Authority Authority { get;  }

        /// <summary>
        /// state indicating if proxy shouldn't try to decrypt 
        /// </summary>
        public bool TunneledOnly { get; set; }

        /// <summary>
        /// Contains the request sent from the proxy to the remote server 
        /// </summary>
        public Request Request { get;  }

        /// <summary>
        /// Contains the response of the remote server to the proxy 
        /// </summary>
        public Response Response { get; } = new Response();

        /// <summary>
        /// Represents timing and size metrics related to this exchange
        /// </summary>
        public ExchangeMetrics Metrics { get; } = new();

        /// <summary>
        /// Connection used by this exchange. The connection object is the connection open between
        /// the proxy and the remote server 
        /// </summary>
        public Connection Connection { get; set; }
        
        /// <summary>
        /// Contains a list of errors 
        /// </summary>
        public List<Error> Errors { get; private set; } = new List<Error>(); 
        

        internal TaskCompletionSource<bool> ExchangeCompletionSource => _exchangeCompletionSource;

        public HeaderField GetMetricsSummaryAsHeader()
        {
            NameValueCollection collection = new NameValueCollection();

            if (Metrics.CreateCertEnd != default)
                collection.Add("create-cert",
                    ((int)
                        (Metrics.CreateCertEnd - Metrics.CreateCertStart).TotalMilliseconds)
                    .ToString());

            if (Connection != null && Connection.SslNegotiationEnd != default)
                collection.Add("SSL", 
                    ((int)
                        (Connection.SslNegotiationEnd - Connection.SslNegotiationStart).TotalMilliseconds)
                    .ToString());

            if (Metrics.RetrievingPool != default)
                collection.Add("time-to-get-a-pool", 
                    ((int)
                        (Metrics.RetrievingPool - Metrics.ReceivedFromProxy).TotalMilliseconds)
                    .ToString());

            if (Metrics.RequestHeaderSent != default)
                collection.Add("Time-to-send", 
                    ((int)
                        (Metrics.RequestHeaderSent - Metrics.ReceivedFromProxy).TotalMilliseconds)
                    .ToString());

            if (Metrics.ResponseHeaderEnd != default)
                collection.Add("TTFB", 
                    ((int)
                        (Metrics.ResponseHeaderEnd - Metrics.RequestHeaderSent).TotalMilliseconds)
                    .ToString());

            return new HeaderField(
                "echoes-metrics",
                $" {string.Join(", ", collection.AllKeys.Select(s => $"({s})={collection[s]}"))}");
        }

        public string FullUrl => Request.Header.GetFullUrl();

        public string KnownAuthority => Request.Header.Authority.ToString();

        public string Method => Request.Header.Method.ToString();

        public string Path => Request.Header.Path.ToString();

        public IEnumerable<HeaderFieldInfo> GetRequestHeaders()
        {
            return Request.Header.Headers.Select(t => (HeaderFieldInfo) t);
        }

        public IEnumerable<HeaderFieldInfo> GetResponseHeaders()
        {
            return Response.Header.Headers.Select(t => (HeaderFieldInfo)t);
        }

        public int StatusCode => Response.Header.StatusCode;
    }


    public class Request
    {
        public Request(RequestHeader header)
        {
            Header = header;
        }

        public RequestHeader Header { get; }

        public Stream Body { get; set; }

        public override string ToString()
        {
            return Header.ToString();
        }
        
    }

    public class Response
    {
        public ResponseHeader Header { get; set; }

        public Stream Body { get; set;  }
    }


    public class Error
    {
        public Error(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        public string Message { get; }

        public Exception Exception { get; }
    }
}