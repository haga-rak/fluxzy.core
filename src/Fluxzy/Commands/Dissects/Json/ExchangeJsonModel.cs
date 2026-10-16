// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Collections.Generic;
using System.Linq;
using Fluxzy.Readers;

namespace Fluxzy.Cli.Commands.Dissects.Json
{
    /// <summary>
    /// Stable, flat projection of an exchange for --json / --ndjson output.
    /// Keep additions backward compatible: scripts and agents depend on this shape.
    /// </summary>
    internal class ExchangeJsonModel
    {
        public int Id { get; init; }

        public int ConnectionId { get; init; }

        public string Method { get; init; } = string.Empty;

        public string Scheme { get; init; } = string.Empty;

        public string Authority { get; init; } = string.Empty;

        public int Port { get; init; }

        public string Path { get; init; } = string.Empty;

        public string Url { get; init; } = string.Empty;

        public string HttpVersion { get; init; } = string.Empty;

        public int Status { get; init; }

        public string? ContentType { get; init; }

        public long RequestBodyLength { get; init; }

        public long ResponseBodyLength { get; init; }

        public long TotalSent { get; init; }

        public long TotalReceived { get; init; }

        public bool Secure { get; init; }

        public bool Pending { get; init; }

        public bool IsWebSocket { get; init; }

        public bool HasPcap { get; init; }

        public DateTime? StartTime { get; init; }

        public double? TtfbMs { get; init; }

        public double? DurationMs { get; init; }

        public string? RemoteAddress { get; init; }

        public string? TlsVersion { get; init; }

        public string? Alpn { get; init; }

        public string? EgressIp { get; init; }

        public List<string> Tags { get; init; } = new();

        public string? Comment { get; init; }

        public ProcessJsonModel? Process { get; init; }

        public List<ClientErrorJsonModel> ClientErrors { get; init; } = new();

        public List<HeaderJsonModel>? RequestHeaders { get; init; }

        public List<HeaderJsonModel>? ResponseHeaders { get; init; }

        public static ExchangeJsonModel Create(ExchangeInfo exchange, ConnectionInfo? connection,
            IArchiveReader reader, bool withHeaders = false)
        {
            var metrics = exchange.Metrics;
            var start = metrics.ReceivedFromProxy;

            return new ExchangeJsonModel {
                Id = exchange.Id,
                ConnectionId = exchange.ConnectionId,
                Method = exchange.Method,
                Scheme = exchange.RequestHeader.Scheme.ToString(),
                Authority = exchange.KnownAuthority,
                Port = exchange.KnownPort,
                Path = exchange.Path,
                Url = exchange.FullUrl,
                HttpVersion = exchange.HttpVersion,
                Status = exchange.StatusCode,
                ContentType = exchange.ContentType,
                RequestBodyLength = Math.Max(0, reader.GetRequestBodyLength(exchange.Id)),
                ResponseBodyLength = Math.Max(0, reader.GetResponseBodyLength(exchange.Id)),
                TotalSent = metrics.TotalSent,
                TotalReceived = metrics.TotalReceived,
                Secure = exchange.Secure,
                Pending = exchange.Pending,
                IsWebSocket = exchange.IsWebSocket,
                HasPcap = connection != null && reader.HasCapture(connection.Id),
                StartTime = ExchangeTimings.Instant(start),
                TtfbMs = ExchangeTimings.TtfbMs(exchange),
                DurationMs = ExchangeTimings.DurationMs(exchange),
                RemoteAddress = connection?.RemoteAddress,
                TlsVersion = connection?.SslInfo?.SslProtocol.ToString(),
                Alpn = connection?.SslInfo?.NegotiatedApplicationProtocol,
                EgressIp = exchange.EgressIp,
                Tags = exchange.Tags.Select(t => t.Value).OrderBy(t => t).ToList(),
                Comment = exchange.Comment,
                Process = exchange.ProcessInfo == null
                    ? null
                    : new ProcessJsonModel {
                        Pid = exchange.ProcessInfo.ProcessId,
                        Path = exchange.ProcessInfo.ProcessPath
                    },
                ClientErrors = exchange.ClientErrors.Select(e => new ClientErrorJsonModel {
                    Code = e.ErrorCode,
                    Message = e.Message
                }).ToList(),
                RequestHeaders = withHeaders ? Headers(exchange.GetRequestHeaders()) : null,
                ResponseHeaders = withHeaders ? Headers(exchange.GetResponseHeaders()) : null
            };
        }

        private static List<HeaderJsonModel>? Headers(IEnumerable<HeaderFieldInfo>? headers)
        {
            return headers?.Select(h => new HeaderJsonModel {
                Name = h.Name.ToString(),
                Value = h.Value.ToString()
            }).ToList();
        }
    }

    internal class HeaderJsonModel
    {
        public string Name { get; init; } = string.Empty;

        public string Value { get; init; } = string.Empty;
    }

    internal class ProcessJsonModel
    {
        public int Pid { get; init; }

        public string? Path { get; init; }
    }

    internal class ClientErrorJsonModel
    {
        public int Code { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
