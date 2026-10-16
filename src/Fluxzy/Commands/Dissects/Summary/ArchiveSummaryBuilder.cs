// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands.Dissects.Summary
{
    internal static class ArchiveSummaryBuilder
    {
        public static ArchiveSummary Build(IArchiveReader reader, string source, string sourceType,
            int maxHosts = 20)
        {
            var exchanges = reader.ReadAllExchanges().ToList();
            var connections = reader.ReadAllConnections().ToList();
            var connectionMap = connections.ToDictionary(c => c.Id, c => c);

            ArchiveMetaInformation? meta = null;

            try {
                meta = reader.ReadMetaInformation();
            }
            catch {
                // Older or partial archives may lack meta. Summary still works.
            }

            var statusClasses = new SortedDictionary<string, int>();
            var statusCodes = new SortedDictionary<int, int>();
            var methods = new SortedDictionary<string, int>();
            var httpVersions = new SortedDictionary<string, int>();
            var contentTypes = new SortedDictionary<string, int>();

            long sent = 0, received = 0, requestBodies = 0, responseBodies = 0;
            int pending = 0, webSocket = 0, secure = 0, withPcap = 0;
            int http4xx = 0, http5xx = 0, proxy = 0, clientErrors = 0;
            var idsWithClientErrors = new List<int>();

            DateTime? first = null, last = null;

            var perHost = new Dictionary<string, HostAccumulator>(StringComparer.OrdinalIgnoreCase);

            foreach (var e in exchanges) {
                var status = e.StatusCode;

                Increment(statusClasses, StatusClass(status));

                if (status > 0) {
                    Increment(statusCodes, status);
                }

                Increment(methods, e.Method);
                Increment(httpVersions, e.HttpVersion);
                Increment(contentTypes, e.ContentType ?? "unknown");

                sent += e.Metrics.TotalSent;
                received += e.Metrics.TotalReceived;
                requestBodies += Math.Max(0, reader.GetRequestBodyLength(e.Id));
                responseBodies += Math.Max(0, reader.GetResponseBodyLength(e.Id));

                if (e.Pending) {
                    pending++;
                }

                if (e.IsWebSocket) {
                    webSocket++;
                }

                if (e.Secure) {
                    secure++;
                }

                if (reader.HasCapture(e.ConnectionId)) {
                    withPcap++;
                }

                if (status is >= 400 and < 500) {
                    http4xx++;
                }

                if (status >= 500) {
                    http5xx++;
                }

                if (status is 528 or 502) {
                    proxy++;
                }

                if (e.ClientErrors.Count > 0) {
                    clientErrors += e.ClientErrors.Count;
                    idsWithClientErrors.Add(e.Id);
                }

                var start = e.Metrics.ReceivedFromProxy;
                var end = e.Metrics.ResponseBodyEnd;

                if (start != default && (first == null || start < first)) {
                    first = start;
                }

                var effectiveEnd = end != default ? end : start;

                if (effectiveEnd != default && (last == null || effectiveEnd > last)) {
                    last = effectiveEnd;
                }

                if (!perHost.TryGetValue(e.KnownAuthority, out var acc)) {
                    acc = perHost[e.KnownAuthority] = new HostAccumulator();
                }

                acc.Count++;
                acc.Received += e.Metrics.TotalReceived;

                if (status >= 400 || e.ClientErrors.Count > 0) {
                    acc.Errors++;
                }

                var headerStart = e.Metrics.ResponseHeaderStart;

                if (start != default && headerStart != default && headerStart >= start) {
                    acc.TtfbSum += (headerStart - start).TotalMilliseconds;
                    acc.TtfbCount++;
                }
            }

            var hosts = perHost
                        .OrderByDescending(kv => kv.Value.Count)
                        .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
                        .Take(maxHosts)
                        .Select(kv => new HostStat {
                            Host = kv.Key,
                            Count = kv.Value.Count,
                            Errors = kv.Value.Errors,
                            Received = kv.Value.Received,
                            AvgTtfbMs = kv.Value.TtfbCount == 0
                                ? null
                                : Math.Round(kv.Value.TtfbSum / kv.Value.TtfbCount, 2)
                        })
                        .ToList();

            int downstreamErrors;

            try {
                downstreamErrors = reader.ReaderAllDownstreamErrors().Count;
            }
            catch {
                downstreamErrors = 0;
            }

            return new ArchiveSummary {
                Source = source,
                SourceType = sourceType,
                FluxzyVersion = meta?.FluxzyVersion,
                ArchiveVersion = meta?.ArchiveVersion,
                CaptureDate = meta?.CaptureDate,
                OperatingSystem = meta?.EnvironmentInformation?.OperationSystem,
                ReverseMode = meta?.CapturedSetting?.ReverseMode,
                Exchanges = new ExchangeStats {
                    Total = exchanges.Count,
                    Pending = pending,
                    WebSocket = webSocket,
                    Secure = secure,
                    WithPcap = withPcap,
                    MinId = exchanges.Count == 0 ? 0 : exchanges.Min(e => e.Id),
                    MaxId = exchanges.Count == 0 ? 0 : exchanges.Max(e => e.Id)
                },
                Connections = new ConnectionStats {
                    Total = connections.Count,
                    WithPcap = connections.Count(c => reader.HasCapture(c.Id)),
                    Tls = connections.Count(c => c.SslInfo != null)
                },
                Time = new TimeSpanStats {
                    First = first,
                    Last = last,
                    DurationSeconds = first != null && last != null
                        ? Math.Round((last.Value - first.Value).TotalSeconds, 3)
                        : null
                },
                Bytes = new ByteStats {
                    Sent = sent,
                    Received = received,
                    RequestBodies = requestBodies,
                    ResponseBodies = responseBodies
                },
                StatusClasses = statusClasses,
                StatusCodes = statusCodes,
                Methods = methods,
                HttpVersions = httpVersions,
                ContentTypes = contentTypes,
                Hosts = hosts,
                Errors = new ErrorStats {
                    Http4xx = http4xx,
                    Http5xx = http5xx,
                    Proxy = proxy,
                    ClientErrors = clientErrors,
                    DownstreamErrors = downstreamErrors,
                    ExchangeIdsWithClientErrors = idsWithClientErrors
                }
            };
        }

        private static string StatusClass(int status)
        {
            if (status <= 0) {
                return "none";
            }

            return $"{status / 100}xx";
        }

        private static void Increment<TKey>(IDictionary<TKey, int> map, TKey key) where TKey : notnull
        {
            map[key] = map.TryGetValue(key, out var v) ? v + 1 : 1;
        }

        private class HostAccumulator
        {
            public int Count;

            public int Errors;

            public long Received;

            public double TtfbSum;

            public int TtfbCount;
        }
    }
}
