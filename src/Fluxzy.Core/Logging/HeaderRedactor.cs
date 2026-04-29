// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Text;

namespace Fluxzy.Logging
{
    internal static class HeaderRedactor
    {
        public static readonly HashSet<string> DefaultRedactedHeaders =
            new(StringComparer.OrdinalIgnoreCase) {
                "Authorization",
                "Proxy-Authorization",
                "Cookie",
                "Set-Cookie",
                "X-Auth-Token"
            };

        public static string FormatHeaders(
            IEnumerable<HeaderFieldInfo>? headers, FluxzySetting? setting)
        {
            if (headers == null)
                return string.Empty;

            var includeSensitive = setting?.LogIncludeSensitiveHeaders ?? false;
            var redactSet = setting?.LogRedactedHeaders ?? DefaultRedactedHeaders;

            var sb = new StringBuilder();

            foreach (var h in headers) {
                var name = h.Name.ToString();

                if (sb.Length > 0)
                    sb.Append('\n');

                sb.Append(name).Append('=');

                if (!includeSensitive && redactSet.Contains(name)) {
                    sb.Append("<redacted, len=").Append(h.Value.Length).Append('>');
                }
                else {
                    sb.Append(h.Value.Span);
                }
            }

            return sb.ToString();
        }
    }
}
