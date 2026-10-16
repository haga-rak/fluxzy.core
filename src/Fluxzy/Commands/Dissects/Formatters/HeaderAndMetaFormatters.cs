// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Fluxzy.Cli.Commands.Dissects.Formatters
{
    internal abstract class HeaderListFormatter : IDissectionFormatter<EntryInfo>
    {
        public abstract string Indicator { get; }

        protected abstract IEnumerable<HeaderFieldInfo>? Select(EntryInfo payload);

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return Write(payload, stdOutWriter, null);
        }

        public async Task Write(EntryInfo payload, StreamWriter stdOutWriter, string? argument)
        {
            var headers = Select(payload);

            if (headers == null) {
                return;
            }

            if (argument != null) {
                var values = headers
                             .Where(h => h.Name.Span.Equals(argument, StringComparison.OrdinalIgnoreCase))
                             .Select(h => h.Value.ToString());

                await stdOutWriter.WriteAsync(string.Join(", ", values));

                return;
            }

            var first = true;

            foreach (var header in headers) {
                if (!first) {
                    await stdOutWriter.WriteLineAsync();
                }

                first = false;
                await stdOutWriter.WriteAsync($"{header.Name}: {header.Value}");
            }
        }
    }

    internal class RequestHeadersFormatter : HeaderListFormatter
    {
        public override string Indicator => "request-headers";

        protected override IEnumerable<HeaderFieldInfo>? Select(EntryInfo payload)
        {
            return payload.Exchange.GetRequestHeaders();
        }
    }

    internal class ResponseHeadersFormatter : HeaderListFormatter
    {
        public override string Indicator => "response-headers";

        protected override IEnumerable<HeaderFieldInfo>? Select(EntryInfo payload)
        {
            return payload.Exchange.GetResponseHeaders();
        }
    }

    internal class RequestHeaderFormatter : RequestHeadersFormatter
    {
        public override string Indicator => "request-header";
    }

    internal class ResponseHeaderFormatter : ResponseHeadersFormatter
    {
        public override string Indicator => "response-header";
    }

    internal class SimpleFormatter : IDissectionFormatter<EntryInfo>
    {
        private readonly Func<EntryInfo, string?> _selector;

        public SimpleFormatter(string indicator, Func<EntryInfo, string?> selector)
        {
            Indicator = indicator;
            _selector = selector;
        }

        public string Indicator { get; }

        public Task Write(EntryInfo payload, StreamWriter stdOutWriter)
        {
            return stdOutWriter.WriteAsync(_selector(payload) ?? string.Empty);
        }
    }

    internal static class MetaFormatters
    {
        public static IEnumerable<IDissectionFormatter<EntryInfo>> All()
        {
            yield return new SimpleFormatter("ttfb", p => Ms(ExchangeTimings.TtfbMs(p.Exchange)));
            yield return new SimpleFormatter("duration", p => Ms(ExchangeTimings.DurationMs(p.Exchange)));

            yield return new SimpleFormatter("start-time",
                p => ExchangeTimings.Instant(p.Exchange.Metrics.ReceivedFromProxy)?.ToString("O"));

            yield return new SimpleFormatter("connection-id", p => p.Exchange.ConnectionId.ToString());
            yield return new SimpleFormatter("remote-ip", p => p.Connection?.RemoteAddress);
            yield return new SimpleFormatter("tls-version", p => p.Connection?.SslInfo?.SslProtocol.ToString());
            yield return new SimpleFormatter("alpn", p => p.Connection?.SslInfo?.NegotiatedApplicationProtocol);

            yield return new SimpleFormatter("tags",
                p => string.Join(",", p.Exchange.Tags.Select(t => t.Value).OrderBy(t => t)));

            yield return new SimpleFormatter("comment", p => p.Exchange.Comment);
        }

        private static string? Ms(double? value)
        {
            return value?.ToString(CultureInfo.InvariantCulture);
        }
    }
}
