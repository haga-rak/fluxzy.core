using System.Collections.Generic;
using System.Text.Json.Serialization;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Utils;

namespace Fluxzy
{
    public class ExchangeInfo : IExchange
    {
        [JsonPropertyOrder(-10)]
        public int ConnectionId { get; }
        
        [JsonPropertyOrder(-9)]
        public int Id { get; }

        public RequestHeaderInfo RequestHeader { get; }

        public ResponseHeaderInfo? ResponseHeader { get; }

        public ExchangeMetrics Metrics { get; }

        public string? ContentType => HeaderUtility.GetSimplifiedContentType(this);

        public bool Done => ResponseHeader?.StatusCode > 0;

        public bool Pending { get; }

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
            Pending = !exchange.Complete.IsCompleted;
            Comment = exchange.Comment;
            Tags = exchange.Tags ?? new HashSet<Tag>();
            IsWebSocket = exchange.IsWebSocket;
            WebSocketMessages = exchange.WebSocketMessages;
        }

        [JsonConstructor]
        public ExchangeInfo(int id, int connectionId, string httpVersion,
            RequestHeaderInfo requestHeader, ResponseHeaderInfo? responseHeader,
            ExchangeMetrics metrics, string egressIp, bool pending, string? comment, HashSet<Tag>? tags,
            bool isWebSocket, List<WsMessage> webSocketMessages, Agent? agent)
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
            Tags = tags ?? new HashSet<Tag>();
        }

        public string HttpVersion { get; }

        public string FullUrl => RequestHeader.GetFullUrl();

        public string KnownAuthority => RequestHeader.Authority.ToString();

        public string Method => RequestHeader.Method.ToString();

        public string Path => RequestHeader.Path.ToString();

        public IEnumerable<HeaderFieldInfo> GetRequestHeaders()
        {
            return RequestHeader.Headers;
        }

        public IEnumerable<HeaderFieldInfo>? GetResponseHeaders()
        {
            return ResponseHeader?.Headers;
        }


        public int StatusCode => ResponseHeader?.StatusCode ?? 0;

        public string? EgressIp { get; }

        public string? Comment { get; set; }

        public HashSet<Tag> Tags { get; }

        public bool IsWebSocket { get; }

        public List<WsMessage>? WebSocketMessages { get; }
        
        public Agent? Agent { get; }
    }

    public class BodyContent
    {
        public int Length { get; set; }
    }
}
