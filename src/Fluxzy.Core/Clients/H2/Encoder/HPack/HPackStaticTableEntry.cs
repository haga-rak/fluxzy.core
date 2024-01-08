// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.Linq;
using Fluxzy.Clients.H2.Encoder.Utils;

namespace Fluxzy.Clients.H2.Encoder.HPack
{
    internal class HPackStaticTableEntry
    {
        static HPackStaticTableEntry()
        {
            StaticTable = new[] {
                new(":authority"),
                new HeaderField(":method", "GET"),
                new HeaderField(":method", "POST"),
                new HeaderField(":path", "/"),
                new HeaderField(":path", "/index.html"),
                new HeaderField(":scheme", "http"),
                new HeaderField(":scheme", "https"),
                new HeaderField(":status", "200"),
                new HeaderField(":status", "204"),
                new HeaderField(":status", "206"),
                new HeaderField(":status", "304"),
                new HeaderField(":status", "400"),
                new HeaderField(":status", "404"),
                new HeaderField(":status", "500"),
                new HeaderField("accept-charset"),
                new HeaderField("accept-encoding", "gzip, deflate"),
                new HeaderField("accept-language"),
                new HeaderField("accept-ranges"),
                new HeaderField("accept"),
                new HeaderField("access-control-allow-origin"),
                new HeaderField("age"),
                new HeaderField("allow"),
                new HeaderField("authorization"),
                new HeaderField("cache-control"),
                new HeaderField("content-disposition"),
                new HeaderField("content-encoding"),
                new HeaderField("content-language"),
                new HeaderField("content-length"),
                new HeaderField("content-location"),
                new HeaderField("content-range"),
                new HeaderField("content-type"),
                new HeaderField("cookie"),
                new HeaderField("date"),
                new HeaderField("etag"),
                new HeaderField("expect"),
                new HeaderField("expires"),
                new HeaderField("from"),
                new HeaderField("host"),
                new HeaderField("if-match"),
                new HeaderField("if-modified-since"),
                new HeaderField("if-none-match"),
                new HeaderField("if-range"),
                new HeaderField("if-unmodified-since"),
                new HeaderField("last-modified"),
                new HeaderField("link"),
                new HeaderField("location"),
                new HeaderField("max-forwards"),
                new HeaderField("proxy-authenticate"),
                new HeaderField("proxy-authorization"),
                new HeaderField("range"),
                new HeaderField("referer"),
                new HeaderField("refresh"),
                new HeaderField("retry-after"),
                new HeaderField("server"),
                new HeaderField("set-cookie"),
                new HeaderField("strict-transport-security"),
                new HeaderField("transfer-encoding"),
                new HeaderField("user-agent"),
                new HeaderField("vary"),
                new HeaderField("via"),
                new HeaderField("www-authenticate")
            };

            ReverseStaticTable = StaticTable.Select((t, index) => new { Entry = t, Index = index })
                                            .ToDictionary(t => t.Entry, t => t.Index, new TableEntryComparer());
        }

        public static HeaderField[] StaticTable { get; }

        public static Dictionary<HeaderField, int> ReverseStaticTable { get; }
    }
}
