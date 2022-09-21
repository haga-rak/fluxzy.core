// Copyright © 2022 Haga Rakotoharivelo

using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Fluxzy.Clients;

namespace Fluxzy
{
    public class ResponseHeaderInfo
    {
        [JsonConstructor]
        public ResponseHeaderInfo(int statusCode, IEnumerable<HeaderFieldInfo> headers)
        {
            StatusCode = statusCode;
            Headers = headers;
        }

        public ResponseHeaderInfo(ResponseHeader originalHeader)
        {
            StatusCode = originalHeader.StatusCode;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s)); 
        }

        public int StatusCode { get;  } 

        public IEnumerable<HeaderFieldInfo> Headers { get; }

    }
}