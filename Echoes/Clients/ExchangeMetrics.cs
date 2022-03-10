// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Echoes.Clients
{
    public class ExchangeMetrics
    {
        public DateTime ReceivedFromProxy { get; set; }

        public DateTime RetrievingPool { get; set; }

        public DateTime RequestHeaderSending { get; set; }

        public DateTime RequestHeaderSent { get; set; }

        public DateTime RequestBodySent { get; set; }

        public DateTime ResponseHeaderStart { get; set; }

        public DateTime ResponseHeaderEnd { get; set; }

        public DateTime ResponseBodyStart { get; set; }
        
        public DateTime ResponseBodyEnd { get; set; }

        public DateTime RemoteClosed { get; set; }

        public DateTime CreateCertStart { get; set; }

        public DateTime CreateCertEnd { get; set; }

        public long TotalSent { get; set; }

        public long TotalReceived { get; set; }
    }
}