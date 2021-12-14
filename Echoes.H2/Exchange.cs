// Copyright © 2021 Haga Rakotoharivelo

using System;
using System.Collections.Generic;
using System.IO;
using Echoes.Encoding;

namespace Echoes.H2
{



    public class Exchange
    {
        public Request Request { get; set; }

        public Response Response { get; set; }

        public ExchangeMetrics Metrics { get; set; }

        public int ConnectionId { get; set; }

        public Stream BaseStream { get; set; }
    }

    public class Connection
    {
        public Authority Authority { get; set; }

        public DateTime ConnectionOpen { get; set; }

        public DateTime ConnectionClosed { get; set; }
    }

    public class Request
    {
        public ICollection<HeaderField> Headers { get;  }

        public Stream Body { get;  }

    }

    public class Response
    {
        public ICollection<HeaderField> Headers { get;  }

        public Stream Body { get;  }
    }


    public class ExchangeMetrics
    {
        public DateTime ReceivedFromProxy { get; set; }

        public DateTime DnsSolveStart { get; set; }

        public DateTime DnsSolveEnd { get; set; }

        public DateTime ConnectionInitiated { get; set; }

        public DateTime ConnectionOpen { get; set; }

        public DateTime SslNegotiationStart { get; set; }

        public DateTime SslNegotiationEnd { get; set; }

        public DateTime RequestHeaderSent { get; set; }

        public DateTime RequestBodySent { get; set; }

        public DateTime ResponseHeaderStart { get; set; }

        public DateTime ResponseHeaderEnd { get; set; }

        public DateTime ResponseBodyStart { get; set; }
        
        public DateTime ResponseBodyEnd { get; set; }
    }
}