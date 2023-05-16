// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Fluxzy.Clients;
using MessagePack;

namespace Fluxzy
{
    [MessagePackObject]
    public class ResponseHeaderInfo
    {
        protected bool Equals(ResponseHeaderInfo other)
        {
            return StatusCode == other.StatusCode && Headers.SequenceEqual(other.Headers);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != this.GetType())
                return false;

            return Equals((ResponseHeaderInfo) obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(StatusCode, Headers);
        }

        [JsonConstructor]
        [SerializationConstructor]
        public ResponseHeaderInfo(int statusCode, IEnumerable<HeaderFieldInfo> headers)
        {
            StatusCode = statusCode;
            Headers = headers;
        }

        public ResponseHeaderInfo(int statusCode, IEnumerable<HeaderFieldInfo> headers, 
            bool computePseudoHeader)
        {
            StatusCode = statusCode;
            Headers = headers;

            if (!computePseudoHeader)
                return;

            var actualHeaders = Headers.ToList();

            if (!actualHeaders.Any(h => h.Name.Span.Equals(
                    ":status".AsSpan(),
                    StringComparison.OrdinalIgnoreCase))) {
                actualHeaders.Add(new HeaderFieldInfo(":status", statusCode.ToString()));
            }

            Headers = actualHeaders;
        }

        public ResponseHeaderInfo(ResponseHeader originalHeader, bool doNotForwardConnectionHeader = false)
        {
            StatusCode = originalHeader.StatusCode;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s, doNotForwardConnectionHeader));
        }

        [Key(0)]
        public int StatusCode { get; }

        [Key(1)]
        public IEnumerable<HeaderFieldInfo> Headers { get; }
    }
}
