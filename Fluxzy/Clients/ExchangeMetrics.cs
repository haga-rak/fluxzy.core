// Copyright © 2022 Haga Rakotoharivelo

using System;

namespace Fluxzy.Clients
{
    public class ExchangeMetrics
    {
        public bool ReusingConnection { get; set; }

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

        public DateTime ErrorInstant { get; set; }

        public long TotalSent { get; set; }

        public long TotalReceived { get; set; }

        public int RequestHeaderLength { get; set; }
        
        public int ResponseHeaderLength { get; set; }

        public int LocalPort { get; set; }

        public string? LocalAddress { get; set; }
    }
}
