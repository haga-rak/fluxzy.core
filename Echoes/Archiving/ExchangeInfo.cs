﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Echoes.Clients;
using Echoes.Clients.H2.Encoder;

namespace Echoes
{
    public class ExchangeInfo : IExchange
    {
        [JsonConstructor]
        public ExchangeInfo()
        {

        }

        public ExchangeInfo(Exchange exchange)
        {
            Id = exchange.Id;
            HttpVersion = exchange.HttpVersion;
            ConnectionId = exchange.Connection?.Id ?? 0;
            Metrics = exchange.Metrics;  
            ResponseHeader = exchange.Response?.Header == null ? default : new ResponseHeaderInfo(exchange.Response.Header);
            RequestHeader = new RequestHeaderInfo(exchange.Request.Header);
            EgressIp = exchange.EgressIp; 
        }

        public int Id { get; set; }

        public int ConnectionId { get; set; }

        public string HttpVersion { get; set; }

        public RequestHeaderInfo RequestHeader { get; set; }
        
        public ResponseHeaderInfo ResponseHeader { get; set; }
        
        public ExchangeMetrics Metrics { get; set; }

        public string FullUrl => RequestHeader.GetFullUrl();

        public string KnownAuthority => RequestHeader.Authority.ToString();

        public string Method => RequestHeader.Method.ToString();

        public string Path => RequestHeader.Path.ToString();

        public IEnumerable<HeaderFieldInfo> GetRequestHeaders()
        {
            return RequestHeader.Headers;
        }

        public IEnumerable<HeaderFieldInfo> GetResponseHeaders()
        {
            return ResponseHeader.Headers;
        }

        public int StatusCode => ResponseHeader.StatusCode;

        public string EgressIp { get; set; }
    }

    public class BodyContent
    {
        public int Length { get; set; }
    }
}