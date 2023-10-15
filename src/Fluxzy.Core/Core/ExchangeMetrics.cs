// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;

namespace Fluxzy.Core
{
    /// <summary>
    /// An object holding metrics information about an exchange
    /// </summary>
    public class ExchangeMetrics
    {
        /// <summary>
        /// true if the exchange reused an existing connection
        /// </summary>
        public bool ReusingConnection { get; set; }

        /// <summary>
        /// Instant received from client 
        /// </summary>
        public DateTime ReceivedFromProxy { get; set; }

        /// <summary>
        /// Instant obtaining an HTTP connection pool
        /// </summary>
        public DateTime RetrievingPool { get; set; }

        /// <summary>
        /// Instant request header was about to be sent 
        /// </summary>
        public DateTime RequestHeaderSending { get; set; }

        /// <summary>
        /// Instant request header was sent
        /// </summary>
        public DateTime RequestHeaderSent { get; set; }

        /// <summary>
        /// Instant request body was sent
        /// </summary>
        public DateTime RequestBodySent { get; set; }

        /// <summary>
        /// Instant first byte of response header was received
        /// </summary>
        public DateTime ResponseHeaderStart { get; set; }

        /// <summary>
        /// Instant last byte of response header was received
        /// </summary>
        public DateTime ResponseHeaderEnd { get; set; }

        /// <summary>
        /// Instant first byte of response body was received
        /// </summary>
        public DateTime ResponseBodyStart { get; set; }

        /// <summary>
        /// Instant last byte of response body was received
        /// </summary>
        public DateTime ResponseBodyEnd { get; set; }

        /// <summary>
        /// Instant the remote closed the connection. DateTime.MinValue if never happens during the capture
        /// </summary>
        public DateTime RemoteClosed { get; set; }

        public DateTime CreateCertStart { get; set; }
        
        public DateTime CreateCertEnd { get; set; }

        /// <summary>
        /// Instant an error was declared
        /// </summary>
        public DateTime ErrorInstant { get; set; }

        /// <summary>
        /// Total byte sent for this exchange
        /// </summary>
        public long TotalSent { get; set; }

        /// <summary>
        /// Total byte received for this exchange
        /// </summary>
        public long TotalReceived { get; set; }

        /// <summary>
        /// Full request header length
        /// </summary>
        public int RequestHeaderLength { get; set; }

        /// <summary>
        /// Full response header length
        /// </summary>
        public int ResponseHeaderLength { get; set; }

        /// <summary>
        /// Port used by proxy client
        /// </summary>
        public int DownStreamClientPort { get; set; }

        /// <summary>
        /// Address used by proxy client
        /// </summary>
        public string DownStreamClientAddress { get; set; } = string.Empty;

        /// <summary>
        /// The endpoint port that receive the client request
        /// </summary>
        public int DownStreamLocalPort { get; set; }

        /// <summary>
        /// The endpoint address that receive the client request
        /// </summary>
        public string DownStreamLocalAddress { get; set; } = string.Empty;

    }
}
