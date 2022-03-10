// Copyright © 2022 Haga Rakotoharivelo

using System;
using Echoes.Clients;

namespace Echoes.Desktop.Common.Models
{
    public class ExchangeViewModel 
    {
        public ExchangeViewModel(Exchange exchange, string sessionId)
        {
            SessionId = sessionId;
            Id = exchange.Id;
            Method = exchange.Request.Header.Method.ToString();
            Url =
                new Uri($"{exchange.Request.Header.Scheme}://{exchange.Request.Header.Authority}{exchange.Request.Header.Path}")
                    .ToString();
            Protocol = exchange.HttpVersion;

            if (exchange.Response?.Header != null)
            {
                StatusCode = exchange.Response?.Header.StatusCode; 
            }

            Done = exchange.Complete.IsCompleted;
        }

        public int Id { get; set; }

        public string SessionId { get; set; }

        public string Method { get; set; } 

        public string Url { get; set; }

        public string Protocol { get; set; }

        public int ? StatusCode { get; set; }

        public string FormattedSize { get; set; }

        public bool Done { get; set; }
    }
}