// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
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

        public ResponseHeaderInfo(ResponseHeader originalHeader, bool doNotForwardConnectionHeader = false)
        {
            StatusCode = originalHeader.StatusCode;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s, doNotForwardConnectionHeader));
        }

        public int StatusCode { get; }

        public IEnumerable<HeaderFieldInfo> Headers { get; }
    }
}
