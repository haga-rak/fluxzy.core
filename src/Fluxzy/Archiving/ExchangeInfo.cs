// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Utils;
using MessagePack;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local (Used by serialization)

namespace Fluxzy
{
    [MessagePackObject]
    public class ExchangeInfo : IExchange, IExchangeLine
    {
        [SerializationConstructor]
#pragma warning disable CS8618 // Used by messagepack
        private ExchangeInfo()
#pragma warning restore CS8618
        {

        }

        public ExchangeInfo(Exchange exchange)
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
            
        }

        [JsonConstructor]
        public ExchangeInfo(
            int id, int connectionId, string httpVersion,
            RequestHeaderInfo requestHeader, ResponseHeaderInfo? responseHeader,
            ExchangeMetrics metrics,
            string egressIp, bool pending, string? comment, HashSet<Tag>? tags,
            bool isWebSocket, List<WsMessage> webSocketMessages,
            Agent? agent, List<ClientError> clientErrors, string knownAuthority, int knownPort, bool secure)
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
        }

        [JsonPropertyOrder(-10)]
        [Key(0)]
        public int ConnectionId { get; private set;  }

        [JsonPropertyOrder(-9)]
        [Key(1)]
        public int Id { get; private set; }

        [Key(2)]
        public RequestHeaderInfo RequestHeader { get; private set; }

        [Key(3)]
        public ResponseHeaderInfo? ResponseHeader { get; private set; }

        [Key(4)]
        public ExchangeMetrics Metrics { get; private set; }

        [IgnoreMember]
        public string? ContentType => HeaderUtility.GetSimplifiedContentType(this);

        [IgnoreMember]
        public long Received => Metrics.TotalReceived;

        [IgnoreMember]
        public long Sent => Metrics.TotalSent;

        /// <summary>
        /// Misleading
        /// </summary>
        [IgnoreMember]
        public bool Done => ResponseHeader?.StatusCode > 0;

        [Key(5)]
        public bool Pending { get; private set; }

        [Key(6)]
        public string HttpVersion { get; private set; }

        [IgnoreMember]
        public string FullUrl => RequestHeader.GetFullUrl();

        [Key(7)]
        public string KnownAuthority { get; private set; }

        [Key(8)]
        public int KnownPort { get; private set; }

        [Key(9)]
        public bool Secure { get; private set; }

        [IgnoreMember]
        public string Method => RequestHeader.Method.ToString();

        [IgnoreMember]
        public string Path => RequestHeader.GetPathOnly();

        public IEnumerable<HeaderFieldInfo> GetRequestHeaders()
        {
            return RequestHeader.Headers;
        }

        public IEnumerable<HeaderFieldInfo>? GetResponseHeaders()
        {
            return ResponseHeader?.Headers;
        }

        [IgnoreMember]
        public int StatusCode => ResponseHeader?.StatusCode ?? 0;

        [Key(10)]
        public string? EgressIp { get; private set; }

        [Key(11)]
        public string? Comment { get; set; }

        [Key(12)]
        public HashSet<Tag> Tags { get; private set; }

        [Key(13)]
        public bool IsWebSocket { get; private set; }

        [Key(14)]
        public List<WsMessage>? WebSocketMessages { get; private set; }

        [Key(15)]
        public Agent? Agent { get; private set; }

        [Key(16)]
        public List<ClientError> ClientErrors { get; private set; }
    }

    //public class BodyContent
    //{
    //    public int Length { get; set; }
    //}
}
