using System;
using System.Collections.Generic;
using System.IO;
using Echoes.H2.Encoder;

namespace Echoes.Archiving.Abstractions
{
    public class ExchangeArchive
    {
        public Dictionary<int, ExchangeInfo> Exchanges { get; set; } = new();

        public Dictionary<int, ConnectionInfo> Connections { get; set; } = new();
    }


    public class ExchangeInfo
    {
        public int Id { get; set; }

        public int ConnectionId { get; set; }

        public IRequestHeader RequestHeader { get; set; }

        public BodyContent RequestContent { get; set; }

        public IResponseHeader ResponseHeader { get; set; }

        public BodyContent ResponseContent { get; set; }

        public ExchangeMetrics Metrics { get; set; }
    }

    public class BodyContent
    {
        public string ContentId { get; set; }

        public int Length { get; set; }
    }


    public interface IHeader
    {
        IEnumerable<HeaderField> Headers { get;  }
    }

    public interface IRequestHeader : IHeader
    {
        ReadOnlyMemory<char> Method { get;  }

        ReadOnlyMemory<char> Path { get;  }

        ReadOnlyMemory<char> Authority { get;  }
    }
    
    public interface IResponseHeader : IHeader
    { 
        int StatusCode { get;  }
    }

    public class RequestInfo
    {
        public RequestHeader Header { get; set; }
    }

    public class ResponseInfo
    {

    }
}