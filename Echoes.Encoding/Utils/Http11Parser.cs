using System;
using System.Collections.Generic;
using System.Linq;
using Echoes.Encoding.HPack;
using Echoes.Encoding.Utils.Interfaces;

namespace Echoes.Encoding.Utils
{
    /// <summary>
    /// Converts a flat HTTP/1.1 request to a list of (name, value) headers compatible with HTTP/2
    /// </summary>
    public class Http11Parser
    {
        private readonly int _maxHeaderLine;
        private readonly IMemoryProvider<char> _memoryProvider;

        private static readonly HashSet<char> LineSeparators = new HashSet<char>(new[] { '\r', '\n' }); 
        private static readonly HashSet<char> SpaceSeparators = new HashSet<char>(new[] { ' ', '\t' });
        private static readonly HashSet<char> HeaderSeparator = new HashSet<char>(new[] { ':' });
        private static readonly HashSet<char> CookieSeparators = new HashSet<char>(new[] { ';' });

        private static readonly ReadOnlyMemory<char> MethodVerb = ":method".AsMemory(); 
        private static readonly ReadOnlyMemory<char> SchemeVerb = ":scheme".AsMemory(); 
        private static readonly ReadOnlyMemory<char> AuthorityVerb = ":authority".AsMemory(); 
        private static readonly ReadOnlyMemory<char> PathVerb = ":path".AsMemory();
        private static readonly ReadOnlyMemory<char> StatusVerb = ":status".AsMemory();
      
        private static readonly ReadOnlyMemory<char> HttpsVerb = "https".AsMemory(); 
        private static readonly ReadOnlyMemory<char> HttpVerb = "http".AsMemory(); 
        private static readonly ReadOnlyMemory<char> HostVerb = "host".AsMemory(); 
        private static readonly ReadOnlyMemory<char> CookieVerb = "cookie".AsMemory();
        private static readonly ReadOnlyMemory<char> ConnectionVerb = "connection".AsMemory();
        private static readonly ReadOnlyMemory<char> TransfertEncodingVerb = "transfert-encoding".AsMemory();
        private static readonly ReadOnlyMemory<char> KeepAliveVerb = "keep-alive".AsMemory();
        private static readonly ReadOnlyMemory<char> ProxyAuthenticate = "proxy-authenticate".AsMemory();
        private static readonly ReadOnlyMemory<char> Trailer = "trailer".AsMemory();
        private static readonly ReadOnlyMemory<char> Upgrade = "upgrade".AsMemory();

        private static readonly HashSet<ReadOnlyMemory<char>> AvoidAutoParseHttp11Headers =
            new HashSet<ReadOnlyMemory<char>>(new[]
            {
                MethodVerb, SchemeVerb, AuthorityVerb, PathVerb, CookieVerb, StatusVerb
            }, new SpanCharactersIgnoreCaseComparer());

        private static readonly HashSet<ReadOnlyMemory<char>> ControlHeaders =
            new HashSet<ReadOnlyMemory<char>>(new[]
            {
                MethodVerb, SchemeVerb, AuthorityVerb, PathVerb, StatusVerb
            }, new SpanCharactersIgnoreCaseComparer());

        private static readonly HashSet<ReadOnlyMemory<char>> NonH2Header =
            new HashSet<ReadOnlyMemory<char>>(new[]
            {
                ConnectionVerb, TransfertEncodingVerb,KeepAliveVerb,ProxyAuthenticate,Trailer,Upgrade
            }, new SpanCharactersIgnoreCaseComparer());

        public Http11Parser(int maxHeaderLine, IMemoryProvider<char> memoryProvider)
        {
            _maxHeaderLine = maxHeaderLine;
            _memoryProvider = memoryProvider;
        }

        public IEnumerable<HeaderField> Read(ReadOnlyMemory<char> input, bool isHttps = true)
        {
            bool firstLine = true;
            
            foreach (var line in input.Split(LineSeparators).ToArray())
            {
                if (firstLine)
                {
                    // parsing request line

                    var arrayOfValue = line.Split(SpaceSeparators).ToArray();

                    if (arrayOfValue.Length == 3)
                    {
                        if (arrayOfValue[0].Length >= 4 && arrayOfValue[0].Slice(0, 4).Span.Equals("HTTP".AsSpan(), StringComparison.OrdinalIgnoreCase))
                        {
                            // Response header block 
                            yield return new HeaderField(StatusVerb, arrayOfValue[1]);
                        }
                        else
                        {
                            // Request header block

                            yield return new HeaderField(MethodVerb, arrayOfValue[0]);
                            yield return new HeaderField(PathVerb, arrayOfValue[1].RemoveProtocolAndAuthority()); // Remove prefix on path
                            yield return new HeaderField(SchemeVerb, isHttps ? HttpsVerb : HttpVerb);

                            if (SchemeVerb.Span.StartsWith(HttpsVerb.Span))
                                isHttps = true;
                        }
                    }

                    firstLine = false;

                    continue; 
                }

                var kpValue = line.Split(HeaderSeparator,2).ToArray();


                if (kpValue.Length != 2)
                    throw new HPackCodecException($"Invalid header on line {line}");


                var headerName = kpValue[0].Trim();

                if (NonH2Header.Contains(headerName))
                    continue; 

                var headerValue = kpValue[1].Trim();

                if (headerName.Span.Equals(HostVerb.Span, StringComparison.OrdinalIgnoreCase))
                {
                    yield return new HeaderField(AuthorityVerb, headerValue);
                    continue; 
                }

                if (headerName.Span.Equals(CookieVerb.Span, StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var cookieEntry in headerValue.Split(CookieSeparators))
                    {
                        yield return new HeaderField(CookieVerb, cookieEntry.Trim());
                    }

                    continue; 
                }
                
                yield return new HeaderField(headerName, headerValue);
            }

        }

        public Span<char> Write(
            ICollection<HeaderField> entries, 
            Span<char> buffer)
        {
            Span<char> cookieBuffer = stackalloc char[_maxHeaderLine];
            var length = InternalWrite(entries, buffer, cookieBuffer);
            return buffer.Slice(0, length);
        }

        private static int InternalWrite(in ICollection<HeaderField> entries, in Span<char> buffer, in Span<char> cookieBuffer)
        {
            var mapping = entries
                .Where(t => ControlHeaders.Contains(t.Name))
                .ToDictionary
                    (t => t.Name, t => t, SpanCharactersIgnoreCaseComparer.Default);

            int totalWritten = 0;
            var offsetBuffer = buffer;

            if (!mapping.TryGetValue(MethodVerb , out var method))
            {
                if (!mapping.TryGetValue(StatusVerb, out var statusHeader))
                {
                    throw new HPackCodecException("Invalid HTTP header. Could not find :method or :status");
                }

                // Response header 

                offsetBuffer = offsetBuffer.Concat("HTTP/1.1 ", ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(statusHeader.Value.Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(" ", ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(Http11Constants.GetStatusLine(statusHeader.Value).Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat("\r\n", ref totalWritten);
            }
            else
            {
                // Request Header

                if (!mapping.TryGetValue(PathVerb, out var path))
                    throw new HPackCodecException("Could not find path verb");

                if (!mapping.TryGetValue(AuthorityVerb, out var authority))
                    throw new HPackCodecException("Could not find authority verb");
                
                offsetBuffer = offsetBuffer.Concat(method.Value.Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(' ', ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(path.Value.Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(" HTTP/1.1\r\n", ref totalWritten);

                offsetBuffer = offsetBuffer.Concat("Host: ", ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(authority.Value.Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat("\r\n", ref totalWritten);
            }

            foreach (var entry in entries)
            {
                if (AvoidAutoParseHttp11Headers.Contains(entry.Name))
                    continue; // PSEUDO headers

                offsetBuffer = offsetBuffer.Concat(entry.Name.Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(": ", ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(entry.Value.Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat("\r\n", ref totalWritten);
            }

            var cookieValue = SpanCharsHelper.Join(
                entries.Where(c =>
                    c.Name.Span.Equals(CookieVerb.Span, StringComparison.OrdinalIgnoreCase)
                ).Select(s => s.Value), "; ".AsSpan(), cookieBuffer);

            if (!cookieValue.IsEmpty)
            {
                offsetBuffer = offsetBuffer.Concat(CookieVerb.Span, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(": ", ref totalWritten);
                offsetBuffer = offsetBuffer.Concat(cookieValue, ref totalWritten);
                offsetBuffer = offsetBuffer.Concat("\r\n", ref totalWritten);
            }

            offsetBuffer = offsetBuffer.Concat("\r\n", ref totalWritten);
            
            return totalWritten;
        }
    }
}