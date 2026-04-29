// Copyright 2021 - Haga Rakotoharivelo - https://github.com/haga-rak

using System;
using System.Diagnostics;
using Fluxzy.Clients;
using Fluxzy.Clients.H11;
using Fluxzy.Clients.H2;
using Fluxzy.Clients.Mock;
using Fluxzy.Core;
using Fluxzy.Extensions;

namespace Fluxzy.Logging
{
    internal static class FluxzyActivitySource
    {
        public const string SourceName = "Fluxzy.Core";

        public static readonly ActivitySource Instance = new(
            SourceName,
            typeof(FluxzyActivitySource).Assembly.GetName().Version?.ToString() ?? "0.0.0");

        public static Activity? StartExchangeActivity(Exchange exchange, Guid proxyInstanceId)
        {
            var traceparent = exchange.GetRequestHeaderValue("traceparent");
            var tracestate = exchange.GetRequestHeaderValue("tracestate");

            Activity? activity;
            if (!string.IsNullOrEmpty(traceparent)
                && ActivityContext.TryParse(traceparent, tracestate, out var parentContext)) {
                activity = Instance.StartActivity(
                    "HTTP " + exchange.Method, ActivityKind.Server, parentContext);
            }
            else {
                activity = Instance.StartActivity(
                    "HTTP " + exchange.Method, ActivityKind.Server);
            }

            if (activity == null)
                return null;

            activity.SetTag("http.request.method", exchange.Method);
            activity.SetTag("url.full", exchange.FullUrl);
            activity.SetTag("server.address", exchange.Authority.HostName);
            activity.SetTag("server.port", exchange.Authority.Port);
            activity.SetTag("fluxzy.exchange_id", exchange.Id);
            activity.SetTag("fluxzy.proxy.instance_id", proxyInstanceId);

            var ua = exchange.GetRequestHeaderValue("User-Agent");
            if (ua != null)
                activity.SetTag("user_agent.original", ua);

            var clientIp = exchange.Metrics.DownStreamClientAddress;
            if (!string.IsNullOrEmpty(clientIp)) {
                activity.SetTag("client.address", clientIp);
                activity.SetTag("client.port", exchange.Metrics.DownStreamClientPort);
            }

            return activity;
        }

        public static void TagDnsResolved(Activity? activity, double dnsMs, bool wasForced)
        {
            if (activity == null)
                return;

            activity.SetTag("fluxzy.dns.duration_ms", dnsMs);
            activity.SetTag("fluxzy.dns.forced", wasForced);
        }

        public static void TagPoolResolved(Activity? activity, IHttpConnectionPool pool, bool reused)
        {
            if (activity == null)
                return;

            activity.SetTag("fluxzy.pool.type", PoolTypeName(pool));
            activity.SetTag("fluxzy.pool.reused", reused);
        }

        public static void TagResponseHeader(Activity? activity, int statusCode)
        {
            if (activity == null || statusCode == 0)
                return;

            activity.SetTag("http.response.status_code", statusCode);
        }

        public static void EnrichOnComplete(Activity? activity, Exchange exchange)
        {
            if (activity == null)
                return;

            if (!string.IsNullOrEmpty(exchange.HttpVersion))
                activity.SetTag("network.protocol.version", exchange.HttpVersion);

            var status = exchange.Response.Header?.StatusCode ?? 0;

            if (status > 0)
                activity.SetTag("http.response.status_code", status);

            var requestBodyBytes = exchange.Metrics.TotalSent - exchange.Metrics.RequestHeaderLength;
            if (requestBodyBytes > 0)
                activity.SetTag("http.request.body.size", requestBodyBytes);

            var responseBodyBytes = exchange.Metrics.TotalReceived - exchange.Metrics.ResponseHeaderLength;
            if (responseBodyBytes > 0)
                activity.SetTag("http.response.body.size", responseBodyBytes);

            if (exchange.Errors.Count > 0 || exchange.Context.Abort)
                activity.SetStatus(ActivityStatusCode.Error);
            else if (status >= 500)
                activity.SetStatus(ActivityStatusCode.Error);
            else if (status >= 200 && status < 400)
                activity.SetStatus(ActivityStatusCode.Ok);
        }

        private static string PoolTypeName(IHttpConnectionPool pool)
        {
            return pool switch {
                Http11ConnectionPool => "Http11",
                H2ConnectionPool => "H2",
                MockedConnectionPool => "Mocked",
                TunnelOnlyConnectionPool => "Tunnel",
                WebsocketConnectionPool => "Websocket",
                _ => pool.GetType().Name
            };
        }
    }
}
