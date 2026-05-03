// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Net;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Extensions;
using Microsoft.Extensions.Logging;

namespace Fluxzy.Logging
{
#pragma warning disable SYSLIB1015 // Argument not referenced from message template — emitted as structured property only
    internal static partial class FluxzyLogEvents
    {
        [LoggerMessage(EventId = 1001, Level = LogLevel.Debug,
            Message = "Client connection accepted (concurrent={ConcurrentCount}, closeImmediately={CloseImmediately})")]
        public static partial void ClientConnectionAccepted(
            ILogger logger,
            int ConcurrentCount,
            bool CloseImmediately);

        [LoggerMessage(EventId = 1002, Level = LogLevel.Debug,
            Message = "Resolving request {Method} {FullUrl}")]
        public static partial void RequestResolutionStarted(
            ILogger logger,
            string Method,
            string FullUrl,
            bool IsSecure,
            bool IsWebSocket,
            bool HasRequestBody,
            long RequestContentLength,
            string? UserAgent,
            int? ProcessId,
            string? ProcessPath);

        [LoggerMessage(EventId = 1003, Level = LogLevel.Debug,
            Message = "DNS {HostName} -> {RemoteIp} in {DnsMs}ms via {DnsResolver}")]
        public static partial void DnsResolved(
            ILogger logger,
            string HostName,
            string RemoteIp,
            int RemotePort,
            double DnsMs,
            string DnsResolver,
            bool WasForced,
            string? UpstreamProxyHost,
            int? UpstreamProxyPort);

        [LoggerMessage(EventId = 1004, Level = LogLevel.Debug,
            Message = "Pool {PoolType} (reused={ReusingConnection}, getPoolMs={GetPoolMs}ms)")]
        public static partial void ConnectionPoolResolved(
            ILogger logger,
            string PoolType,
            bool ReusingConnection,
            double GetPoolMs,
            int? ConnectionId,
            string? RemoteIp,
            int? RemotePort,
            string? LocalIp,
            int? LocalPort,
            string? Alpn,
            string? TlsProtocol,
            string? CipherSuite,
            string? SniSent,
            double? TlsHandshakeMs,
            double? TcpConnectMs,
            bool IsBlindTunnel,
            bool IsMocked);

        [LoggerMessage(EventId = 1005, Level = LogLevel.Debug,
            Message = "Sending request on connection {ConnectionId}")]
        public static partial void RequestSending(
            ILogger logger,
            int ConnectionId,
            int RequestHeaderLength,
            bool HasExpectContinue,
            bool HasRequestBody,
            long RequestContentLength,
            bool Chunked,
            int RequestProcessedOnConnection);

        [LoggerMessage(EventId = 1006, Level = LogLevel.Debug,
            Message = "Request sent in {SendMs}ms ({BytesSent} bytes)")]
        public static partial void RequestSent(
            ILogger logger,
            int ConnectionId,
            long BytesSent,
            long RequestBodyBytes,
            double SendMs,
            double HeaderSendMs,
            double BodySendMs,
            bool EarlyResponse);

        [LoggerMessage(EventId = 1007, Level = LogLevel.Debug,
            Message = "Response {StatusCode} ({TtfbMs}ms TTFB)")]
        public static partial void ResponseHeaderReceived(
            ILogger logger,
            int ConnectionId,
            int StatusCode,
            string? ReasonPhrase,
            int ResponseHeaderLength,
            long ResponseContentLength,
            bool ResponseChunked,
            bool ConnectionCloseRequest,
            double TtfbMs,
            double ResponseHeaderReadMs,
            bool HasResponseBody,
            string? ContentEncoding,
            string? ContentType,
            string? Server);

        [LoggerMessage(EventId = 1008, Level = LogLevel.Debug,
            Message = "Exchange done {StatusCode} totalMs={TotalMs} sent={TotalSent} recv={TotalReceived}")]
        public static partial void ExchangeCompleted(
            ILogger logger,
            int ConnectionId,
            int StatusCode,
            string FullUrl,
            double TotalMs,
            double DnsMs,
            double GetPoolMs,
            double TcpConnectMs,
            double TlsHandshakeMs,
            double SendMs,
            double TtfbMs,
            double ResponseBodyMs,
            long TotalSent,
            long TotalReceived,
            int RequestHeaderLength,
            int ResponseHeaderLength,
            bool ReusingConnection,
            int RequestProcessedOnConnection,
            int ErrorCount,
            bool Aborted,
            bool ClosedRemote);

        [LoggerMessage(EventId = 1009, Level = LogLevel.Debug,
            Message = "Pool evicted {Authority} (reason={Reason})")]
        public static partial void ConnectionEvicted(
            ILogger logger,
            string Authority,
            string Reason,
            int? ConnectionId);

        [LoggerMessage(EventId = 1010, Level = LogLevel.Debug,
            Message = "Connection opened to {RemoteIp}:{RemotePort} ({HttpVersion}, tls={TlsProtocol}, handshakeMs={TlsHandshakeMs})")]
        public static partial void ConnectionOpened(
            ILogger logger,
            int ConnectionId,
            string Authority,
            string RemoteIp,
            int RemotePort,
            int LocalPort,
            string HttpVersion,
            string? Alpn,
            string? TlsProtocol,
            string? CipherSuite,
            string? SniSent,
            double TcpConnectMs,
            double TlsHandshakeMs,
            double ProxyConnectMs,
            bool ViaUpstreamProxy);

        [LoggerMessage(EventId = 1099, Level = LogLevel.Trace,
            Message = "Exchange envelope {ExchangeId}")]
        public static partial void ExchangeEnvelope(
            ILogger logger,
            int ExchangeId,
            string RequestHeaders,
            string? ResponseHeaders,
            string? Trailers);

        [LoggerMessage(EventId = 2001, Level = LogLevel.Warning,
            Message = "Client connection init failed from {RemoteEndPoint}")]
        public static partial void ClientConnectionInitFailed(
            ILogger logger,
            Exception exception,
            string RemoteEndPoint);

        [LoggerMessage(EventId = 2002, Level = LogLevel.Warning,
            Message = "Certificate resolution failed for {Host}")]
        public static partial void CertificateResolutionFailed(
            ILogger logger,
            Exception exception,
            string Host);

        [LoggerMessage(EventId = 3001, Level = LogLevel.Error,
            Message = "Unhandled connection processing error from {RemoteEndPoint}")]
        public static partial void ConnectionProcessingError(
            ILogger logger,
            Exception exception,
            string RemoteEndPoint);

        public static void LogRequestResolutionStarted(ILogger logger, Exchange exchange)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var body = exchange.Request.Body;
            var hasReqBody = body != null && (!body.CanSeek || body.Length > 0);

            RequestResolutionStarted(logger,
                Method: exchange.Method,
                FullUrl: exchange.FullUrl,
                IsSecure: exchange.Authority.Secure,
                IsWebSocket: exchange.IsWebSocket,
                HasRequestBody: hasReqBody,
                RequestContentLength: exchange.Request.Header.ContentLength,
                UserAgent: exchange.GetRequestHeaderValue("User-Agent"),
                ProcessId: exchange.ProcessInfo?.ProcessId,
                ProcessPath: exchange.ProcessInfo?.ProcessPath);
        }

        public static void LogDnsResolved(
            ILogger logger,
            Exchange exchange,
            string hostName,
            IPAddress remoteIp,
            int remotePort,
            DateTime dnsSolveStart,
            DateTime dnsSolveEnd,
            IDnsSolver dnsSolver,
            bool wasForced)
        {
            var dnsMs = wasForced ? 0 : Ms(dnsSolveStart, dnsSolveEnd);

            FluxzyActivitySource.TagDnsResolved(exchange.LogActivity, dnsMs, wasForced);

            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var proxy = exchange.Context.ProxyConfiguration;

            DnsResolved(logger,
                HostName: hostName,
                RemoteIp: remoteIp.ToString(),
                RemotePort: remotePort,
                DnsMs: dnsMs,
                DnsResolver: dnsSolver.GetType().Name,
                WasForced: wasForced,
                UpstreamProxyHost: proxy?.Host,
                UpstreamProxyPort: proxy?.Port);
        }

        public static void LogConnectionPoolResolved(ILogger logger, Exchange exchange, IHttpConnectionPool pool)
        {
            FluxzyActivitySource.TagPoolResolved(exchange.LogActivity, pool, exchange.Metrics.ReusingConnection);

            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var c = exchange.Connection;
            var ssl = c?.SslInfo;

            var poolType = pool switch {
                Http11ConnectionPool => "Http11",
                H2ConnectionPool => "H2",
                MockedConnectionPool => "Mocked",
                TunnelOnlyConnectionPool => "Tunnel",
                WebsocketConnectionPool => "Websocket",
                _ => pool.GetType().Name
            };

            ConnectionPoolResolved(logger,
                PoolType: poolType,
                ReusingConnection: exchange.Metrics.ReusingConnection,
                GetPoolMs: Ms(exchange.Metrics.ReceivedFromProxy, exchange.Metrics.RetrievingPool),
                ConnectionId: c?.Id,
                RemoteIp: c?.RemoteAddress?.ToString(),
                RemotePort: c == null ? null : exchange.Authority.Port,
                LocalIp: c?.LocalAddress,
                LocalPort: c?.LocalPort,
                Alpn: ssl?.NegotiatedApplicationProtocol,
                TlsProtocol: ssl?.SslProtocol.ToString(),
                CipherSuite: ssl?.NegotiatedCipherSuite.ToString(),
                SniSent: c == null || !exchange.Authority.Secure ? null : exchange.Authority.HostName,
                TlsHandshakeMs: c == null ? null : Ms(c.SslNegotiationStart, c.SslNegotiationEnd),
                TcpConnectMs: c == null ? null : Ms(c.TcpConnectionOpening, c.TcpConnectionOpened),
                IsBlindTunnel: pool is TunnelOnlyConnectionPool,
                IsMocked: pool is MockedConnectionPool);
        }

        public static void LogRequestSending(ILogger logger, Exchange exchange)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var c = exchange.Connection;
            var body = exchange.Request.Body;
            var hasReqBody = body != null && (!body.CanSeek || body.Length > 0);

            RequestSending(logger,
                ConnectionId: c?.Id ?? 0,
                RequestHeaderLength: exchange.Metrics.RequestHeaderLength,
                HasExpectContinue: exchange.Request.Header.HasExpectContinue,
                HasRequestBody: hasReqBody,
                RequestContentLength: exchange.Request.Header.ContentLength,
                Chunked: exchange.Request.Header.ChunkedBody,
                RequestProcessedOnConnection: c?.RequestProcessed ?? 0);
        }

        public static void LogRequestSent(ILogger logger, Exchange exchange, bool earlyResponse)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var m = exchange.Metrics;
            var bytesSent = m.TotalSent;
            var bodyBytes = bytesSent - m.RequestHeaderLength;

            RequestSent(logger,
                ConnectionId: exchange.Connection?.Id ?? 0,
                BytesSent: bytesSent,
                RequestBodyBytes: bodyBytes < 0 ? 0 : bodyBytes,
                SendMs: Ms(m.RequestHeaderSending, m.RequestBodySent),
                HeaderSendMs: Ms(m.RequestHeaderSending, m.RequestHeaderSent),
                BodySendMs: Ms(m.RequestHeaderSent, m.RequestBodySent),
                EarlyResponse: earlyResponse);
        }

        public static void LogResponseHeaderReceived(ILogger logger, Exchange exchange)
        {
            FluxzyActivitySource.TagResponseHeader(
                exchange.LogActivity, exchange.Response.Header?.StatusCode ?? 0);

            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var m = exchange.Metrics;
            var header = exchange.Response.Header;

            ResponseHeaderReceived(logger,
                ConnectionId: exchange.Connection?.Id ?? 0,
                StatusCode: header?.StatusCode ?? 0,
                ReasonPhrase: null,
                ResponseHeaderLength: m.ResponseHeaderLength,
                ResponseContentLength: header?.ContentLength ?? -1,
                ResponseChunked: header?.ChunkedBody ?? false,
                ConnectionCloseRequest: header?.ConnectionCloseRequest ?? false,
                TtfbMs: Ms(m.RequestHeaderSending, m.ResponseHeaderEnd),
                ResponseHeaderReadMs: Ms(m.ResponseHeaderStart, m.ResponseHeaderEnd),
                HasResponseBody: header != null
                    && header.HasResponseBody(exchange.Request.Header.Method.Span, out _),
                ContentEncoding: exchange.GetResponseHeaderValue("Content-Encoding"),
                ContentType: exchange.GetResponseHeaderValue("Content-Type"),
                Server: exchange.GetResponseHeaderValue("Server"));
        }

        public static void LogExchangeCompleted(ILogger logger, Exchange exchange, FluxzySetting? setting = null)
        {
            var activity = exchange.LogActivity;
            if (activity != null) {
                FluxzyActivitySource.EnrichOnComplete(activity, exchange);
                activity.Dispose();
                exchange.LogActivity = null;
            }

            if (logger.IsEnabled(LogLevel.Trace)) {
                LogExchangeEnvelope(logger, exchange, setting);
            }

            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var m = exchange.Metrics;
            var c = exchange.Connection;

            ExchangeCompleted(logger,
                ConnectionId: c?.Id ?? 0,
                StatusCode: exchange.Response.Header?.StatusCode ?? 0,
                FullUrl: exchange.FullUrl,
                TotalMs: Ms(m.ReceivedFromProxy, m.ResponseBodyEnd),
                DnsMs: c == null ? 0 : Ms(c.DnsSolveStart, c.DnsSolveEnd),
                GetPoolMs: Ms(m.ReceivedFromProxy, m.RetrievingPool),
                TcpConnectMs: c == null ? 0 : Ms(c.TcpConnectionOpening, c.TcpConnectionOpened),
                TlsHandshakeMs: c == null ? 0 : Ms(c.SslNegotiationStart, c.SslNegotiationEnd),
                SendMs: Ms(m.RequestHeaderSending, m.RequestBodySent),
                TtfbMs: Ms(m.RequestHeaderSending, m.ResponseHeaderEnd),
                ResponseBodyMs: Ms(m.ResponseBodyStart, m.ResponseBodyEnd),
                TotalSent: m.TotalSent,
                TotalReceived: m.TotalReceived,
                RequestHeaderLength: m.RequestHeaderLength,
                ResponseHeaderLength: m.ResponseHeaderLength,
                ReusingConnection: m.ReusingConnection,
                RequestProcessedOnConnection: c?.RequestProcessed ?? 0,
                ErrorCount: exchange.Errors.Count,
                Aborted: exchange.Context.Abort,
                ClosedRemote: m.RemoteClosed != default);
        }

        public static void LogExchangeEnvelope(ILogger logger, Exchange exchange, FluxzySetting? setting)
        {
            var requestHeaders = HeaderRedactor.FormatHeaders(exchange.GetRequestHeaders(), setting);
            var responseHeaders = HeaderRedactor.FormatHeaders(exchange.GetResponseHeaders(), setting);

            var trailers = exchange.GetResponseTrailers();
            var trailerString = trailers == null
                ? null
                : HeaderRedactor.FormatHeaders(trailers, setting);

            ExchangeEnvelope(logger,
                ExchangeId: exchange.Id,
                RequestHeaders: requestHeaders,
                ResponseHeaders: responseHeaders,
                Trailers: trailerString);
        }

        public static void LogConnectionEvicted(ILogger logger, IHttpConnectionPool pool, string reason)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            int? connectionId = pool switch {
                H2ConnectionPool h2 => h2.Id,
                _ => null
            };

            ConnectionEvicted(logger,
                Authority: pool.Authority.ToString(),
                Reason: reason,
                ConnectionId: connectionId);
        }

        public static void LogConnectionOpened(ILogger logger, Connection connection, bool viaUpstreamProxy)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
                return;

            var ssl = connection.SslInfo;

            ConnectionOpened(logger,
                ConnectionId: connection.Id,
                Authority: connection.Authority.ToString(),
                RemoteIp: connection.RemoteAddress?.ToString() ?? string.Empty,
                RemotePort: connection.Authority.Port,
                LocalPort: connection.LocalPort,
                HttpVersion: connection.HttpVersion ?? string.Empty,
                Alpn: ssl?.NegotiatedApplicationProtocol,
                TlsProtocol: ssl?.SslProtocol.ToString(),
                CipherSuite: ssl?.NegotiatedCipherSuite.ToString(),
                SniSent: connection.Authority.Secure ? connection.Authority.HostName : null,
                TcpConnectMs: Ms(connection.TcpConnectionOpening, connection.TcpConnectionOpened),
                TlsHandshakeMs: Ms(connection.SslNegotiationStart, connection.SslNegotiationEnd),
                ProxyConnectMs: Ms(connection.ProxyConnectStart, connection.ProxyConnectEnd),
                ViaUpstreamProxy: viaUpstreamProxy);
        }

        private static double Ms(DateTime start, DateTime end)
        {
            return start == default || end == default ? 0 : (end - start).TotalMilliseconds;
        }
    }
#pragma warning restore SYSLIB1015
}
