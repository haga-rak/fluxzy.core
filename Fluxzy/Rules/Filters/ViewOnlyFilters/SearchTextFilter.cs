﻿using System;
using System.Buffers;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using Fluxzy.Misc;
using Fluxzy.Misc.Streams;
using YamlDotNet.Serialization;

namespace Fluxzy.Rules.Filters.ViewOnlyFilters
{
    /// <summary>
    /// Perform a global filter on any part of exchange (header, body).
    /// This is a view filter only 
    /// </summary>
    public class SearchTextFilter : Filter
    {
        [JsonIgnore]
        [YamlIgnore]
        private byte[]? _patternBuffer; 
        
        public SearchTextFilter(string pattern)
        {
            Pattern = pattern;
        }

        public override FilterScope FilterScope => FilterScope.ResponseBodyReceivedFromRemote;

        public override Guid Identifier => (GetType().Name + HashCode.Combine(Pattern, SearchInRequestHeader, SearchInResponseHeader, SearchInRequestBody, SearchInResponseBody)).GetMd5Guid();

        public bool SearchInRequestHeader { get; set; } = true;

        public bool SearchInResponseHeader { get; set; } = true;

        public bool SearchInRequestBody { get; set; } = false; 
        
        public bool SearchInResponseBody { get; set; } = false;

        public bool CaseSensitive { get; set; } = true; 

        public string Pattern { get; set; }
        
        public override string AutoGeneratedName => string.IsNullOrWhiteSpace(Pattern) ? "Search" : $"Search `{Pattern}`";

        public override string ShortName => "q";

        private byte[] GetPatternBuffer()
        {
            if (_patternBuffer != null)
                return _patternBuffer;

            return _patternBuffer = Encoding.UTF8.GetBytes(Pattern);
        }
        
        protected override bool InternalApply(IAuthority authority, 
            IExchange? exchange, IFilteringContext? filteringContext)
        {
            if (exchange is not ExchangeInfo exchangeInfo)
            {
                return false; 
            }

            var searchString = Pattern.AsSpan();

            if (SearchInRequestHeader)
            {
                // TODO add check full Url 

                var maxHeaderLength = exchangeInfo.RequestHeader.Headers.Sum(s => s.Name.Length + s.Value.Length + 2);

                char[]? heapBuffer = null;
                Span<char> headerLineBuffer = maxHeaderLength < 1024 ? stackalloc char[maxHeaderLength] :
                    heapBuffer = ArrayPool<char>.Shared.Rent(maxHeaderLength);

                try
                {
                    foreach (var header in exchangeInfo.RequestHeader.Headers)
                    {
                        if (Contains(header, searchString, ref headerLineBuffer, CaseSensitive))
                        {
                            return true;
                        }
                    }
                }
                finally
                {
                    if (heapBuffer != null)
                    {
                        ArrayPool<char>.Shared.Return(heapBuffer);
                    }
                }
            }

            if (SearchInResponseHeader && exchangeInfo.ResponseHeader?.Headers != null)
            {
                var headers = exchangeInfo.ResponseHeader.Headers.ToList();

                // TODO add check full Url 

                var maxHeaderLength = headers.Sum(s => s.Name.Length + s.Value.Length + 2);

                char[]? heapBuffer = null;
                Span<char> headerLineBuffer = maxHeaderLength < 1024 ? stackalloc char[maxHeaderLength] :
                    heapBuffer = ArrayPool<char>.Shared.Rent(maxHeaderLength);

                try
                {
                    foreach (var header in headers)
                    {
                        if (Contains(header, searchString, ref headerLineBuffer, CaseSensitive))
                        {
                            return true;
                        }
                    }
                }
                finally
                {
                    if (heapBuffer != null)
                    {
                        ArrayPool<char>.Shared.Return(heapBuffer);
                    }
                }
            }

            if (SearchInRequestBody && filteringContext != null && filteringContext.HasRequestBody)
            {
                try
                {
                    var bodyLength = filteringContext.Reader.GetRequestBodyLength(exchangeInfo.Id);
                    using var stream = filteringContext.Reader.GetRequestBody(exchangeInfo.Id)!;
                    var caseSensitive = CaseSensitive; 
                    
                    
                    if (SearchOnStream(bodyLength, stream, caseSensitive))
                        return true;
                }
                catch (IOException)
                {
                    // We ignore IOException as the exchange may be in active state
                }
            }

            if (SearchInResponseBody && filteringContext != null && filteringContext.Reader.HasResponseBody(exchangeInfo.Id))
            {
                try
                {
                    var bodyLength = filteringContext.Reader.GetResponseBodyLength(exchangeInfo.Id);
                    using var stream = filteringContext.Reader.GetResponseBody(exchangeInfo.Id)!;
                    var caseSensitive = CaseSensitive;

                    if (SearchOnStream(bodyLength, stream, caseSensitive))
                        return true;
                }
                catch (IOException)
                {
                    // We ignore IOException as the exchange may be in active state
                }
            }

            return false; 
        }

        private bool SearchOnStream(long bodyLength, Stream stream, bool caseSensitive)
        {
            if (bodyLength > 1024 * 512)
            {
                var patternBuffer = GetPatternBuffer();

                // Use SearchStream 
                using var searchStream = new SearchStream(stream, patternBuffer);

                searchStream.Drain();

                if (searchStream.Result?.OffsetFound >= 0)
                    return true;
            }
            else
            {
                var buffer = ArrayPool<byte>.Shared.Rent((int)bodyLength);

                try
                {
                    var actualLength = stream.SeekableStreamToBytes(buffer);

                    // TODO : many things to fix and to improve here 
                    // TODO : 1 - Encoding should be deduced from request header, if not possible fall back to UTF8 
                    // TODO : 2 - Use ArrayPool buffer / stack buffer instead of allocating a brand new string
                    // TODO :     for the entire request body 

                    var str = Encoding.UTF8.GetString(buffer, 0, actualLength);

                    var contains = str.AsSpan().Contains(Pattern.AsSpan(),
                        caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

                    if (contains)
                    {
                        return true;
                    }
                }
                finally
                {
                    if (buffer != null)
                        ArrayPool<byte>.Shared.Return(buffer);
                }

                // Read from buffer 
            }

            return false;
        }

        public static bool Contains(HeaderFieldInfo header, ReadOnlySpan<char> searchString, ref Span<char> headerLineBuffer, bool caseSensitive)
        {
            var headerLength = header.Name.Length + header.Value.Length + 2;

            header.Name.Span.CopyTo(headerLineBuffer);
            headerLineBuffer[header.Name.Length] = ':';
            headerLineBuffer[header.Name.Length + 1] = ' ';
            header.Value.Span.CopyTo(headerLineBuffer.Slice(header.Name.Length + 2));

            ReadOnlySpan<char> searchSpan = headerLineBuffer.Slice(0, headerLength);
            
            var contains = searchSpan.Contains(searchString,
                caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);

            return contains;
        }
    }
}
