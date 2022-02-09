// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Echoes.Encoding;
using Echoes.Encoding.Utils;

namespace Echoes.H2
{
    public class Exchange
    {
        public Exchange(
            Authority authority, 
            ReadOnlyMemory<char> header, 
            Http11Parser parser)
        {
            Authority = authority;
            Request = new Request(new RequestHeader(header, authority.Secure, parser));
        }

        private readonly TaskCompletionSource<bool> _exchangeCompletionSource = new TaskCompletionSource<bool>(); 

        /// <summary>
        /// This tasks indicates the status of the exchange
        /// </summary>
        internal Task<bool> Complete => _exchangeCompletionSource.Task; 

        /// <summary>
        /// The remote authority for this request 
        /// </summary>
        public Authority Authority { get;  }

        /// <summary>
        /// state indicating if proxy shouldn't try to decrypt 
        /// </summary>
        public bool TunneledOnly { get; set; }

        /// <summary>
        /// Contains the request sent from the proxy to the remote server 
        /// </summary>
        public Request Request { get;  }

        /// <summary>
        /// Contains the response of the remote server to the proxy 
        /// </summary>
        public Response Response { get; } = new Response();

        /// <summary>
        /// Represents timing and size metrics related to this exchange
        /// </summary>
        public ExchangeMetrics Metrics { get; private set; } = new ExchangeMetrics();

        /// <summary>
        /// Id of the connection used by this exchange. The connection object is the connection open between
        /// the proxy and the remote server 
        /// </summary>
        public int ConnectionId { get; set; }

        /// <summary>
        /// Stream between the client and the proxy  
        /// </summary>
        public Stream BaseStream { get; set; }

        /// <summary>
        /// Stream between the proxy and the server
        /// </summary>
        public Stream UpStream { get; set; }

        /// <summary>
        /// Contains a list of errors occured in this proxy 
        /// </summary>
        public List<Error> Errors { get; private set; } = new List<Error>(); 
        

        internal TaskCompletionSource<bool> ExchangeCompletionSource => _exchangeCompletionSource;
    }

    /// <summary>
    /// Contains information about transport layer 
    /// </summary>
    public class Connection
    {
        public int ConnectionId { get; set; }

        public Authority Authority { get; set; }

        public DateTime ConnectionOpen { get; set; }

        public DateTime ConnectionClosed { get; set; }
    }


    
    public class Request
    {
        public Request(RequestHeader header)
        {
            Header = header;
        }

        public RequestHeader Header { get; }

        public Stream Body { get; set; }

    }

    public class Response
    {
        public ResponseHeader Header { get; set; }

        public Stream Body { get; set;  }
    }


    public class ExchangeMetrics
    {
        public DateTime ReceivedFromProxy { get; set; }

        public DateTime DnsSolveStart { get; set; }

        public DateTime DnsSolveEnd { get; set; }

        public DateTime ConnectStart { get; set; }

        public DateTime ConnectEnd { get; set; }

        public DateTime SslNegotiationStart { get; set; }

        public DateTime SslNegotiationEnd { get; set; }
        

        public DateTime RequestHeaderSending { get; set; }

        public DateTime RequestHeaderSent { get; set; }

        public DateTime RequestBodySent { get; set; }

        public DateTime ResponseHeaderStart { get; set; }

        public DateTime ResponseHeaderEnd { get; set; }

        public DateTime ResponseBodyStart { get; set; }
        
        public DateTime ResponseBodyEnd { get; set; }

        public DateTime RemoteClosed { get; set; }

        public long TotalSent { get; set; }

        public long TotalReceived { get; set; }
    }

    public class Error
    {
        public Error(string message, Exception exception)
        {
            Message = message;
            Exception = exception;
        }

        public string Message { get; }

        public Exception Exception { get; }
    }
}