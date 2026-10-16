// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;

namespace Fluxzy.Cli.Commands.Dissects.Summary
{
    /// <summary>
    /// Shape of `dissect summary --json`. Keep additions backward compatible.
    /// </summary>
    internal class ArchiveSummary
    {
        public string Source { get; init; } = string.Empty;

        public string SourceType { get; init; } = string.Empty;

        public string? FluxzyVersion { get; init; }

        public string? ArchiveVersion { get; init; }

        public DateTime? CaptureDate { get; init; }

        public string? OperatingSystem { get; init; }

        public bool? ReverseMode { get; init; }

        public ExchangeStats Exchanges { get; init; } = new();

        public ConnectionStats Connections { get; init; } = new();

        public TimeSpanStats Time { get; init; } = new();

        public ByteStats Bytes { get; init; } = new();

        public SortedDictionary<string, int> StatusClasses { get; init; } = new();

        public SortedDictionary<int, int> StatusCodes { get; init; } = new();

        public SortedDictionary<string, int> Methods { get; init; } = new();

        public SortedDictionary<string, int> HttpVersions { get; init; } = new();

        public SortedDictionary<string, int> ContentTypes { get; init; } = new();

        public List<HostStat> Hosts { get; init; } = new();

        public ErrorStats Errors { get; init; } = new();
    }

    internal class ExchangeStats
    {
        public int Total { get; init; }

        public int Pending { get; init; }

        public int WebSocket { get; init; }

        public int Secure { get; init; }

        public int WithPcap { get; init; }

        public int MinId { get; init; }

        public int MaxId { get; init; }
    }

    internal class ConnectionStats
    {
        public int Total { get; init; }

        public int WithPcap { get; init; }

        public int Tls { get; init; }
    }

    internal class TimeSpanStats
    {
        public DateTime? First { get; init; }

        public DateTime? Last { get; init; }

        public double? DurationSeconds { get; init; }
    }

    internal class ByteStats
    {
        public long Sent { get; init; }

        public long Received { get; init; }

        public long RequestBodies { get; init; }

        public long ResponseBodies { get; init; }
    }

    internal class HostStat
    {
        public string Host { get; init; } = string.Empty;

        public int Count { get; init; }

        public int Errors { get; init; }

        public long Received { get; init; }

        public double? AvgTtfbMs { get; init; }
    }

    internal class ErrorStats
    {
        public int Http4xx { get; init; }

        public int Http5xx { get; init; }

        public int Proxy { get; init; }

        public int ClientErrors { get; init; }

        public int DownstreamErrors { get; init; }

        public List<int> ExchangeIdsWithClientErrors { get; init; } = new();
    }
}
