// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Fluxzy.Clients;

namespace Fluxzy
{
    /// <summary>
    ///     This data structure is used for serialization only
    /// </summary>
    public class RequestHeaderInfo
    {
        public RequestHeaderInfo(RequestHeader originalHeader, bool doNotForwardConnectionHeader = false)
        {
            Method = originalHeader.Method;
            Scheme = originalHeader.Scheme;
            Path = originalHeader.Path;
            Authority = originalHeader.Authority;
            Headers = originalHeader.HeaderFields.Select(s => new HeaderFieldInfo(s, doNotForwardConnectionHeader));
        }

        [JsonConstructor]
        public RequestHeaderInfo(
            ReadOnlyMemory<char> method, ReadOnlyMemory<char> scheme, ReadOnlyMemory<char> path,
            ReadOnlyMemory<char> authority, IEnumerable<HeaderFieldInfo> headers)
        {
            Method = method;
            Scheme = scheme;
            Path = path;
            Authority = authority;
            Headers = headers;
        }

        public RequestHeaderInfo(
            string method, string scheme, string path,
            string authority, IEnumerable<HeaderFieldInfo> headers)
            : this (method.AsMemory(), scheme.AsMemory(), path.AsMemory(), authority.AsMemory(), headers)
        {

        }

        public RequestHeaderInfo(
            string method, string fullUrl, IEnumerable<HeaderFieldInfo> headers)
        {
            var uri = new Uri(fullUrl);

            Method = method.AsMemory();
            Scheme = uri.Scheme.AsMemory();
            Path = uri.PathAndQuery.AsMemory();
            Authority = uri.Authority.AsMemory();

            var listHeaders = headers.ToList();

            if (!listHeaders.Any(h => h.Name.Span.Equals(
                    ":method".AsSpan(),
                    StringComparison.OrdinalIgnoreCase))) {

                listHeaders.Add(new HeaderFieldInfo(":method".AsMemory(), Method, true));
                listHeaders.Add(new HeaderFieldInfo(":authority".AsMemory(), Authority, true));
                listHeaders.Add(new HeaderFieldInfo(":path".AsMemory(), Path, true));
                listHeaders.Add(new HeaderFieldInfo(":scheme".AsMemory(), Scheme, true));
            }

            Headers = listHeaders;
        }

        public ReadOnlyMemory<char> Method { get; }

        public ReadOnlyMemory<char> Scheme { get; }

        public ReadOnlyMemory<char> Path { get; }

        public ReadOnlyMemory<char> Authority { get; }

        public IEnumerable<HeaderFieldInfo> Headers { get; }

        public string GetFullUrl()
        {
            var stringPath = Path.ToString();

            if (Uri.TryCreate(Path.ToString(), UriKind.Absolute, out var uri) &&
                uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return stringPath;

            return $"{Scheme}://{Authority}{stringPath}";
        }

        public string GetPathOnly()
        {
            var stringPath = Path.ToString();

            if (Uri.TryCreate(Path.ToString(), UriKind.Absolute, out var uri) &&
                uri.Scheme.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                return uri.PathAndQuery;

            return $"{stringPath}";
        }
    }
}
