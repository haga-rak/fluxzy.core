// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2.Encoder;
using Fluxzy.Misc.Streams;

namespace Fluxzy.Clients
{
    public class Exchange : IExchange
    {
        public int Id { get; }

        /// <summary>
        ///     This tasks indicates the status of the exchange
        /// </summary>
        internal Task<bool> Complete => ExchangeCompletionSource.Task;

        /// <summary>
        ///     The remote authority
        /// </summary>
        public Authority Authority { get; }
        
        /// <summary>
        ///     Contains the request sent from the proxy to the remote server
        /// </summary>
        public Request Request { get; }

        /// <summary>
        ///     Contains the response of the remote server to the proxy
        /// </summary>
        public Response Response { get; } = new();

        /// <summary>
        ///     Represents timing and size metrics related to this exchange
        /// </summary>
        public ExchangeMetrics Metrics { get; } = new();

        /// <summary>
        ///     Connection used by this exchange. The connection object is the connection open between
        ///     the proxy and the remote server
        /// </summary>
        public Connection? Connection { get; set; }

        /// <summary>
        ///     Contains a list of errors
        /// </summary>
        public List<Error> Errors { get; } = new();

        internal TaskCompletionSource<bool> ExchangeCompletionSource { get; } = new();

        public ExchangeContext Context { get; }

        private Exchange(
            IIdProvider idProvider,
            ExchangeContext context,
            Authority authority,
            ReadOnlyMemory<char> requestHeaderPlain,
            Stream requestBody,
            ReadOnlyMemory<char> responseHeader,
            Stream responseBody,
            bool isSecure,
            string httpVersion, DateTime receivedFromProxy)
        {
            Id = idProvider.NextExchangeId();

            Context = context;
            Authority = authority;
            HttpVersion = httpVersion;

            Request = new Request(new RequestHeader(requestHeaderPlain, isSecure))
            {
                Body = requestBody ?? StreamUtils.EmptyStream
            };

            Response = new Response
            {
                Header = new ResponseHeader(responseHeader, isSecure),
                Body = responseBody ?? StreamUtils.EmptyStream
            };

            // TODO : Fill metrics 

            Metrics.ReceivedFromProxy = receivedFromProxy;

            ExchangeCompletionSource.SetResult(false);
        }

        public Exchange(
            IIdProvider idProvider,
            ExchangeContext context,
            Authority authority,
            RequestHeader requestHeader,
            Stream bodyStream,
            string httpVersion,
            DateTime receivedFromProxy)
        {
            Id = idProvider.NextExchangeId();
            Context = context;
            Authority = authority;
            HttpVersion = httpVersion;

            Request = new Request(requestHeader)
            {
                Body = bodyStream
            };

            Metrics.ReceivedFromProxy = receivedFromProxy;
        }

        public Exchange(
            IIdProvider idProvider,
            Authority authority,
            ReadOnlyMemory<char> requestHeaderPlain, string?  httpVersion, DateTime receivedFromProxy)
        {
            Id = idProvider.NextExchangeId();
            Context = new ExchangeContext(authority);
            Authority = authority;
            HttpVersion = httpVersion;
            Request = new Request(new RequestHeader(requestHeaderPlain, authority.Secure));
            Metrics.ReceivedFromProxy = receivedFromProxy;
        }

        public string? HttpVersion { get; set; }

        public bool IsWebSocket => Request.Header.IsWebSocketRequest;

        public List<WsMessage>? WebSocketMessages { get; set; }

        public string FullUrl => Request.Header.GetFullUrl();

        public string KnownAuthority => Authority.HostName;

        public string Method => Request.Header.Method.ToString();

        public string Path => Request.Header.Path.ToString();

        public int StatusCode => Response.Header?.StatusCode ?? 0;

        public string? EgressIp => Connection?.RemoteAddress?.ToString();

        public string? Comment { get; set; } = null;

        public HashSet<Tag>? Tags { get; set; } = null;

        public IEnumerable<HeaderFieldInfo> GetRequestHeaders()
        {
            return Request.Header.Headers.Select(t => (HeaderFieldInfo)t);
        }

        public IEnumerable<HeaderFieldInfo>? GetResponseHeaders()
        {
            return Response.Header?.Headers.Select(t => (HeaderFieldInfo)t);
        }

        public Agent? Agent { get; set; }

        public static Exchange CreateUntrackedExchange(
            IIdProvider idProvider, ExchangeContext context, Authority authority,
            ReadOnlyMemory<char> requestHeaderPlain, Stream requestBody, ReadOnlyMemory<char> responseHeader,
            Stream responseBody, bool isSecure, string httpVersion, DateTime receivedFromProxy)
        {
            return new Exchange(idProvider, context, authority,
                requestHeaderPlain, requestBody, responseHeader, responseBody, isSecure, httpVersion,
                receivedFromProxy);
        }

        /// <summary>
        /// Get performance metrics as header field
        /// </summary>
        /// <returns></returns>
        public HeaderField GetMetricsSummaryAsHeader()
        {
            var collection = new NameValueCollection();

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

        public bool ShouldClose()
        {
            return Request
                   .Header["Connection".AsMemory()].Any(c =>
                       c.Value.Span.Equals("close", StringComparison.OrdinalIgnoreCase));
        }
    }

    public class Request
    {
        public RequestHeader Header { get; }

        public Stream? Body { get; set; }

        public Request(RequestHeader header)
        {
            Header = header;
        }

        public override string ToString()
        {
            return Header.ToString();
        }
    }

    public class Response
    {
        public ResponseHeader? Header { get; set; }

        public Stream? Body { get; set; }
    }

    public class Error
    {
        public string Message { get; }

        public Exception Exception { get; }

        public Error(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }
    }
}
