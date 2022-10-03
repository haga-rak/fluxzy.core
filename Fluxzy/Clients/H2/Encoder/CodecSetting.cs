using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients.H2.Encoder
{
    public class CodecSetting
    {
        /// <summary>
        /// The HPACK encoder will use dynamic table when encoding value of the headers on this list.
        /// </summary>
        private static readonly string [] SavedHeadersStrings =
        {
            ":authority",
            "accept",
            "accept-charset",
            "accept-encoding",
            "accept-language",
            "cookie",
            "user-agent",
            "referer",
            "host",
            "access-control-allow-origin",
            "content-language",
            "Sec-Fetch-Site",
            "Sec-Fetch-Mode",
            "Sec-Fetch-Dest",
            "DNT",
            "sec-ch-ua-mobile",
            "sec-ch-ua",
        };

        /// <summary>
        /// Header name in this list will be registered to the dynamic table when encoding. 
        /// </summary>
        public HashSet<ReadOnlyMemory<char>>
            EncodedHeaders { get; } =
            new HashSet<ReadOnlyMemory<char>>(SavedHeadersStrings.Select(s => s.AsMemory()),
                new SpanCharactersIgnoreCaseComparer()); 
        
        /// <summary>
        /// The maximum header line (request line included)
        /// </summary>
        public int MaxHeaderLineLength { get; set; } = 16384;

        /// <summary>
        /// When encoding, Huffman is applied only if if string length exceed this value. 
        /// </summary>
        public int MaxLengthUncompressedString { get; set; } = 4;


        /// <summary>
        /// Max length for a stackalloc, beyond this value heap allocation is used
        /// </summary>
        public int MaxStackAllocationLength { get; set; } = 4096; 
    }
}