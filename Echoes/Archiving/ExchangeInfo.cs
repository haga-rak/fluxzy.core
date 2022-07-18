using System.Text.Json.Serialization;
using Echoes.Clients;

namespace Echoes
{
    public class ExchangeInfo
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
        }

        public int Id { get; set; }

        public int ConnectionId { get; set; }

        public string HttpVersion { get; set; }

        public RequestHeaderInfo RequestHeader { get; set; }
        
        public ResponseHeaderInfo ResponseHeader { get; set; }
        
        public ExchangeMetrics Metrics { get; set; }

    }

    public class BodyContent
    {
        public int Length { get; set; }
    }
}