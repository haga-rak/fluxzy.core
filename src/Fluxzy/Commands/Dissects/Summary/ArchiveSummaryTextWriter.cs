// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Fluxzy.Cli.Commands.Dissects.Summary
{
    internal static class ArchiveSummaryTextWriter
    {
        public static void Write(ArchiveSummary s, TextWriter w)
        {
            w.WriteLine($"Source          : {s.Source} ({s.SourceType})");

            if (s.FluxzyVersion != null) {
                w.WriteLine($"Fluxzy version  : {s.FluxzyVersion}");
            }

            if (s.CaptureDate != null) {
                w.WriteLine($"Capture date    : {s.CaptureDate:O}");
            }

            if (s.OperatingSystem != null) {
                w.WriteLine($"OS              : {s.OperatingSystem}");
            }

            if (s.ReverseMode == true) {
                w.WriteLine("Mode            : reverse proxy");
            }

            w.WriteLine();
            w.WriteLine($"Exchanges       : {s.Exchanges.Total} (ids {s.Exchanges.MinId}..{s.Exchanges.MaxId}," +
                        $" pending {s.Exchanges.Pending}, websocket {s.Exchanges.WebSocket}," +
                        $" secure {s.Exchanges.Secure}, with pcap {s.Exchanges.WithPcap})");

            w.WriteLine($"Connections     : {s.Connections.Total} (tls {s.Connections.Tls}," +
                        $" with pcap {s.Connections.WithPcap})");

            if (s.Time.First != null) {
                w.WriteLine($"Time span       : {s.Time.First:O} -> {s.Time.Last:O} ({s.Time.DurationSeconds}s)");
            }

            w.WriteLine($"Bytes           : sent {s.Bytes.Sent}, received {s.Bytes.Received}," +
                        $" request bodies {s.Bytes.RequestBodies}, response bodies {s.Bytes.ResponseBodies}");

            w.WriteLine($"Errors          : 4xx {s.Errors.Http4xx}, 5xx {s.Errors.Http5xx}," +
                        $" proxy {s.Errors.Proxy}, client errors {s.Errors.ClientErrors}," +
                        $" downstream errors {s.Errors.DownstreamErrors}");

            if (s.Errors.ExchangeIdsWithClientErrors.Count > 0) {
                w.WriteLine($"  exchanges with client errors: {Join(s.Errors.ExchangeIdsWithClientErrors)}");
            }

            w.WriteLine();
            WriteHistogram(w, "Status", s.StatusClasses.Select(kv => (kv.Key, kv.Value)));
            WriteHistogram(w, "Status codes", s.StatusCodes.Select(kv => (kv.Key.ToString(), kv.Value)));
            WriteHistogram(w, "Methods", s.Methods.Select(kv => (kv.Key, kv.Value)));
            WriteHistogram(w, "HTTP versions", s.HttpVersions.Select(kv => (kv.Key, kv.Value)));
            WriteHistogram(w, "Content types", s.ContentTypes.Select(kv => (kv.Key, kv.Value)));

            w.WriteLine();
            w.WriteLine($"Hosts (top {s.Hosts.Count})");

            var hostWidth = s.Hosts.Count == 0 ? 4 : s.Hosts.Max(h => h.Host.Length);

            w.WriteLine($"  {"host".PadRight(hostWidth)}  {"count",6}  {"errors",6}  {"received",10}  {"avg ttfb",10}");

            foreach (var h in s.Hosts) {
                var ttfb = h.AvgTtfbMs == null ? "-" : $"{h.AvgTtfbMs}ms";
                w.WriteLine($"  {h.Host.PadRight(hostWidth)}  {h.Count,6}  {h.Errors,6}  {h.Received,10}  {ttfb,10}");
            }
        }

        private static void WriteHistogram(TextWriter w, string title, IEnumerable<(string Key, int Count)> items)
        {
            var list = items.ToList();

            if (list.Count == 0) {
                return;
            }

            w.WriteLine($"{title,-16}: {string.Join(", ", list.Select(i => $"{i.Key}={i.Count}"))}");
        }

        private static string Join(IEnumerable<int> ids)
        {
            return string.Join(",", ids);
        }
    }
}
