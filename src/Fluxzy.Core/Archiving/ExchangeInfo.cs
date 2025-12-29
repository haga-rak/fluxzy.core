// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Fluxzy.Clients.H11;
using Fluxzy.Core;
using Fluxzy.Utils;
using Fluxzy.Utils.ProcessTracking;
using MessagePack;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local (Used by serialization)

namespace Fluxzy
{
    /// <summary>
    /// Packed information about an exchange
    /// </summary>
    [MessagePackObject]
    public class ExchangeInfo : IExchange, IExchangeLine
    {
        [SerializationConstructor]
#pragma warning disable CS8618 // Used by messagepack
        private ExchangeInfo()
#pragma warning restore CS8618
        {

        }

        /// <summary>
        /// Create an exchange info from an internal exchange object
        /// </summary>
        /// <param name="exchange"></param>
        internal ExchangeInfo(Exchange exchange)
        {
            Id = exchange.Id;
            HttpVersion = exchange.HttpVersion ?? "-";
            ConnectionId = exchange.Connection?.Id ?? 0;
            Metrics = exchange.Metrics;
            Agent = exchange.Agent;

            ResponseHeader = exchange.Response?.Header == null
                ? default
                : new ResponseHeaderInfo(exchange.Response.Header);

            RequestHeader = new RequestHeaderInfo(exchange.Request.Header);
            EgressIp = exchange.EgressIp;
            Pending = !exchange.Complete.IsCompleted && !exchange.ClientErrors.Any();
            Comment = exchange.Comment;
            Tags = exchange.Tags ?? new HashSet<Tag>();
            IsWebSocket = exchange.IsWebSocket;
            WebSocketMessages = exchange.WebSocketMessages;
            ClientErrors = exchange.ClientErrors;
            KnownAuthority = exchange.KnownAuthority;
            KnownPort = exchange.KnownPort;
            Secure = exchange.Authority.Secure;
            ProcessInfo = exchange.ProcessInfo;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="id"></param>
        /// <param name="connectionId"></param>
        /// <param name="httpVersion"></param>
        /// <param name="requestHeader"></param>
        /// <param name="responseHeader"></param>
        /// <param name="metrics"></param>
        /// <param name="egressIp"></param>
        /// <param name="pending"></param>
        /// <param name="comment"></param>
        /// <param name="tags"></param>
        /// <param name="isWebSocket"></param>
        /// <param name="webSocketMessages"></param>
        /// <param name="agent"></param>
        /// <param name="clientErrors"></param>
        /// <param name="knownAuthority"></param>
        /// <param name="knownPort"></param>
        /// <param name="secure"></param>
        /// <param name="processInfo"></param>
        [JsonConstructor]
        public ExchangeInfo(
            int id, int connectionId, string httpVersion,
            RequestHeaderInfo requestHeader, ResponseHeaderInfo? responseHeader,
            ExchangeMetrics metrics,
            string egressIp, bool pending, string? comment, HashSet<Tag>? tags,
            bool isWebSocket, List<WsMessage> webSocketMessages,
            Agent? agent, List<ClientError> clientErrors, string knownAuthority, int knownPort, bool secure,
            ProcessInfo? processInfo = null)
        {
            Id = id;
            ConnectionId = connectionId;
            HttpVersion = httpVersion;
            RequestHeader = requestHeader;
            ResponseHeader = responseHeader;
            Metrics = metrics;
            EgressIp = egressIp;
            Pending = false;
            Comment = comment;
            IsWebSocket = isWebSocket;
            WebSocketMessages = webSocketMessages;
            Agent = agent;
            ClientErrors = clientErrors;
            KnownAuthority = knownAuthority;
            Tags = tags ?? new HashSet<Tag>();
            KnownPort = knownPort;
            Secure = secure;
            ProcessInfo = processInfo;
        }

        /// <summary>
        /// The connection id 
        /// </summary>
        [JsonPropertyOrder(-10)]
        [Key(0)]
        public int ConnectionId { get; internal set;  }

        /// <summary>
        /// The exchange id
        /// </summary>
        [JsonPropertyOrder(-9)]
        [Key(1)]
        public int Id { get; internal set; }

        /// <summary>
        ///  The request header
        /// </summary>
        [Key(2)]
        public RequestHeaderInfo RequestHeader { get; private set; }

        /// <summary>
        /// The response header
        /// </summary>
        [Key(3)]
        public ResponseHeaderInfo? ResponseHeader { get; private set; }

        /// <summary>
        /// Metrics about this exchange
        /// </summary>
        [Key(4)]
        public ExchangeMetrics Metrics { get; private set; }

        /// <summary>
        /// A simplified response content type of this exchange, values can be:
        /// json, html, css, img, xml, js, font, audio, video, pdf, pbuf, text, xul, zip, bin
        /// </summary>
        [IgnoreMember]
        public string? ContentType => HeaderUtility.GetSimplifiedContentType(this);

        /// <summary>
        /// The total number of bytes received
        /// </summary>
        [IgnoreMember]
        public long Received => Metrics.TotalReceived;
        
        /// <summary>
        /// The total number of bytes sent
        /// </summary>
        [IgnoreMember]
        public long Sent => Metrics.TotalSent;

        /// <summary>
        ///  True if the exchange is complete
        /// </summary>
        [IgnoreMember]
        public bool Done => ResponseHeader?.StatusCode > 0;

        /// <summary>
        /// True if the exchange is pending
        /// </summary>
        [Key(5)]
        public bool Pending { get; private set; }

        /// <summary>
        /// The http version, values possible are HTTP/1.1, HTTP/2
        /// </summary>
        [Key(6)]
        public string HttpVersion { get; private set; }

        /// <summary>
        /// The absolute full url of the request
        /// </summary>
        [IgnoreMember]
        public string FullUrl => RequestHeader.GetFullUrl();

        /// <summary>
        /// The remote hostname 
        /// </summary>
        [Key(7)]
        public string KnownAuthority { get; private set; }

        /// <summary>
        /// The remote port
        /// </summary>
        [Key(8)]
        public int KnownPort { get; private set; }

        /// <summary>
        /// True if exchange is HTTPS 
        /// </summary>
        [Key(9)]
        public bool Secure { get; private set; }

        /// <summary>
        /// The request method
        /// </summary>
        [IgnoreMember]
        public string Method => RequestHeader.Method.ToString();

        /// <summary>
        /// The request path only
        /// </summary>
        [IgnoreMember]
        public string Path => RequestHeader.GetPathOnly();

        /// <summary>
        /// Enumerate request headers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HeaderFieldInfo> GetRequestHeaders()
        {
            return RequestHeader.Headers;
        }

        /// <summary>
        /// Enumerate response headers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<HeaderFieldInfo>? GetResponseHeaders()
        {
            return ResponseHeader?.Headers;
        }

        /// <summary>
        /// The HTTP status code. If no response has been received: 0.
        /// </summary>
        [IgnoreMember]
        public int StatusCode => ResponseHeader?.StatusCode ?? 0;

        /// <summary>
        /// The remote IP address, null if not known 
        /// </summary>
        [Key(10)]
        public string? EgressIp { get; private set; }

        /// <summary>
        /// A comment about about this exchange
        /// </summary>
        [Key(11)]
        public string? Comment { get; set; }

        /// <summary>
        /// Tags (metainformation) on this exchange
        /// </summary>
        [Key(12)]
        public HashSet<Tag> Tags { get; private set; }

        /// <summary>
        /// True if the current exchange is a websocket exchange
        /// </summary>
        [Key(13)]
        public bool IsWebSocket { get; private set; }

        /// <summary>
        /// List of websocket messages if the current exchange is a websocket exchange.
        /// null otherwise
        /// </summary>
        [Key(14)]
        public List<WsMessage>? WebSocketMessages { get; private set; }

        /// <summary>
        /// A friendly information about the client agent. In default implementation, agent is inferred
        /// from user agent. 
        /// </summary>
        [Key(15)]
        public Agent? Agent { get; private set; }

        /// <summary>
        ///  Contains a list of transport errors that occurred during the exchange.
        /// </summary>
        [Key(16)]
        public List<ClientError> ClientErrors { get; private set; }

        /// <summary>
        ///     Information about the local process that initiated this exchange.
        ///     Null if process tracking is disabled or the connection is not from localhost.
        /// </summary>
        [Key(17)]
        public ProcessInfo? ProcessInfo { get; private set; }

        /// <summary>
        /// The string representation of this exchange
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"#{Id} {Method} {FullUrl}";
        }
    }
}
