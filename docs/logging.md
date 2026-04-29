# Logging

Fluxzy.Core emits structured logs through `Microsoft.Extensions.Logging.Abstractions`
and per-exchange traces through `System.Diagnostics.ActivitySource`. Both are wired
through one optional parameter on the `Proxy` constructor.

## Quickstart

```csharp
using Microsoft.Extensions.Logging;

using var loggerFactory = LoggerFactory.Create(builder => builder
    .SetMinimumLevel(LogLevel.Debug)
    .AddSimpleConsole(o => o.IncludeScopes = true));

var setting = FluxzySetting.CreateLocalRandomPort();

await using var proxy = new Proxy(setting, loggerFactory: loggerFactory);
proxy.Run();
```

When `loggerFactory` is null (the default), `NullLoggerFactory.Instance` is used and
no logs are emitted — zero overhead, no behaviour change for existing callers.

For OpenTelemetry traces, register the `Fluxzy.Core` `ActivitySource`:

```csharp
using OpenTelemetry.Trace;

Sdk.CreateTracerProviderBuilder()
   .AddSource("Fluxzy.Core")
   .AddOtlpExporter()
   .Build();
```

If the inbound HTTP request carries a `traceparent` header (W3C Trace Context),
Fluxzy reads it and starts the per-exchange Activity as a child — so traces stitch
end-to-end across upstream callers and downstream origins.

## Logging scopes

Every event is fired inside two nested scopes, so structured-logging backends
(Serilog, Seq, Datadog, etc.) automatically attach the scope properties to each
line — there's no need to repeat them in message templates.

**Connection scope** (one per inbound TCP connection):

| Property            | Meaning                                       |
|---------------------|-----------------------------------------------|
| `ProxyConnectionId` | Per-`Proxy`-instance monotonic counter        |
| `DownstreamRemote`  | Inbound `IPEndPoint` from the client          |
| `DownstreamLocal`   | Inbound `IPEndPoint` Fluxzy is bound to       |

**Exchange scope** (one per HTTP exchange — request/response pair):

| Property      | Meaning                                  |
|---------------|------------------------------------------|
| `ExchangeId`  | `Exchange.Id` (monotonic)                |
| `Authority`   | Target `host:port`                       |
| `Method`      | HTTP method (`GET`, `POST`, …)           |
| `Path`        | Request path                             |
| `HttpVersion` | `HTTP/1.1`, `HTTP/2`, …                  |

## EventId schema

| Range      | Tier                                         |
|------------|----------------------------------------------|
| 1000-1099  | Lifecycle (connection / exchange) — Debug    |
| 1100-1199  | Pool / transport — Debug                     |
| 1200-1299  | TLS — reserved for future                    |
| 1099       | Exchange envelope (full headers) — Trace     |
| 2000-2999  | Warnings                                     |
| 3000-3999  | Errors                                       |

EventIds are stable contract — changes are breaking.

## Event catalogue

All properties listed below are **in addition to** the scope properties above.

### 1001 `ClientConnectionAccepted` — Debug

Fired once per inbound TCP connection, immediately after the connection scope opens.

| Property            | Type   | Meaning                                  |
|---------------------|--------|------------------------------------------|
| `ConcurrentCount`   | `int`  | Concurrent connections being processed   |
| `CloseImmediately`  | `bool` | True when overall-concurrency cap is hit |

### 1002 `RequestResolutionStarted` — Debug

Fired once per processed exchange (skipped for raw CONNECT) right after the
exchange scope opens, before pool resolution.

| Property               | Type      | Meaning                                  |
|------------------------|-----------|------------------------------------------|
| `Method`               | `string`  | HTTP method                              |
| `FullUrl`              | `string`  | Full reconstructed URL                   |
| `IsSecure`             | `bool`    | HTTPS                                    |
| `IsWebSocket`          | `bool`    | WebSocket upgrade request                |
| `HasRequestBody`       | `bool`    | Body present                             |
| `RequestContentLength` | `long`    | -1 when chunked / unknown                |
| `UserAgent`            | `string?` | `User-Agent` header value                |
| `ProcessId`            | `int?`    | Local process id (only when tracking on) |
| `ProcessPath`          | `string?` | Local process path                       |

### 1003 `DnsResolved` — Debug

Fired in `DnsUtility.ComputeDnsUpdateExchange` after DNS resolution.

| Property             | Type      | Meaning                                  |
|----------------------|-----------|------------------------------------------|
| `HostName`           | `string`  | Hostname queried                         |
| `RemoteIp`           | `string`  | Resolved IP                              |
| `RemotePort`         | `int`     | Effective remote port                    |
| `DnsMs`              | `double`  | Resolution latency (0 when forced)       |
| `DnsResolver`        | `string`  | `DefaultDnsResolver`, `DnsOverHttpsResolver`, … |
| `WasForced`          | `bool`    | True when a rule pre-set the IP          |
| `UpstreamProxyHost`  | `string?` | Upstream proxy host (when configured)    |
| `UpstreamProxyPort`  | `int?`    | Upstream proxy port (when configured)    |

### 1004 `ConnectionPoolResolved` — Debug

Fired once per `PoolBuilder.GetPool` success.

| Property               | Type       | Meaning                                  |
|------------------------|------------|------------------------------------------|
| `PoolType`             | `string`   | `Http11` / `H2` / `Mocked` / `Tunnel` / `Websocket` |
| `ReusingConnection`    | `bool`     | Warm reuse vs. fresh handshake           |
| `GetPoolMs`            | `double`   | Time from receive to pool resolution     |
| `ConnectionId`         | `int?`     | Bound `Connection.Id` (null when reused without binding) |
| `RemoteIp` / `RemotePort` | `string?` / `int?` | Resolved upstream endpoint           |
| `LocalIp` / `LocalPort`   | `string?` / `int?` | Bound local endpoint                 |
| `Alpn`                 | `string?`  | ALPN negotiated protocol                 |
| `TlsProtocol`          | `string?`  | `Tls12` / `Tls13` / …                    |
| `CipherSuite`          | `string?`  | Negotiated cipher suite                  |
| `SniSent`              | `string?`  | SNI value (Authority hostname for HTTPS) |
| `TlsHandshakeMs`       | `double?`  | TLS handshake duration                   |
| `TcpConnectMs`         | `double?`  | TCP connect duration                     |
| `IsBlindTunnel`        | `bool`     | Tunnel-only (no decryption)              |
| `IsMocked`             | `bool`     | Mocked response                          |

### 1005 `RequestSending` — Debug

Fired in H1 and H2 send paths immediately before the request header bytes go on the wire.

| Property                       | Type    | Meaning                              |
|--------------------------------|---------|--------------------------------------|
| `ConnectionId`                 | `int`   | Bound upstream `Connection.Id`       |
| `RequestHeaderLength`          | `int`   | Encoded header length (bytes)        |
| `HasExpectContinue`            | `bool`  | Request carries `Expect: 100-continue` |
| `HasRequestBody`               | `bool`  |                                      |
| `RequestContentLength`         | `long`  | -1 when chunked                      |
| `Chunked`                      | `bool`  | Chunked transfer encoding            |
| `RequestProcessedOnConnection` | `int`   | Counter of requests on this connection |

### 1006 `RequestSent` — Debug

Fired after the request body finishes sending (or skipped via early Expect-100 rejection).

| Property            | Type     | Meaning                                  |
|---------------------|----------|------------------------------------------|
| `ConnectionId`      | `int`    |                                          |
| `BytesSent`         | `long`   | Total upstream bytes sent so far         |
| `RequestBodyBytes`  | `long`   | Body bytes (clamped to ≥0)               |
| `SendMs`            | `double` | Header-sending → body-sent total         |
| `HeaderSendMs`      | `double` | Header-sending → header-sent             |
| `BodySendMs`        | `double` | Header-sent → body-sent                  |
| `EarlyResponse`     | `bool`   | True when origin answered before body    |

### 1007 `ResponseHeaderReceived` — Debug

Fired once the final (non-1xx) response header is parsed.

| Property                 | Type      | Meaning                                  |
|--------------------------|-----------|------------------------------------------|
| `ConnectionId`           | `int`     |                                          |
| `StatusCode`             | `int`     |                                          |
| `ReasonPhrase`           | `string?` | Currently null (not stored)              |
| `ResponseHeaderLength`   | `int`     | Bytes of response header                 |
| `ResponseContentLength`  | `long`    | -1 when chunked                          |
| `ResponseChunked`        | `bool`    |                                          |
| `ConnectionCloseRequest` | `bool`    | Origin requested `Connection: close`     |
| `TtfbMs`                 | `double`  | Header-sending → response-header-end     |
| `ResponseHeaderReadMs`   | `double`  | Header-start → header-end                |
| `HasResponseBody`        | `bool`    |                                          |
| `ContentEncoding`        | `string?` | `Content-Encoding` header                |
| `ContentType`            | `string?` | `Content-Type` header                    |
| `Server`                 | `string?` | `Server` header                          |

### 1008 `ExchangeCompleted` — Debug

Fired exactly once per processed exchange, right after `await exchange.Complete`.
This is the single most useful line for postmortem analysis.

| Property                       | Type     | Meaning                              |
|--------------------------------|----------|--------------------------------------|
| `ConnectionId`                 | `int`    |                                      |
| `StatusCode`                   | `int`    |                                      |
| `FullUrl`                      | `string` |                                      |
| `TotalMs`                      | `double` | End-to-end exchange latency          |
| `DnsMs` / `GetPoolMs` / `TcpConnectMs` / `TlsHandshakeMs` | `double` | Phase latencies |
| `SendMs` / `TtfbMs` / `ResponseBodyMs`                    | `double` | Phase latencies |
| `TotalSent` / `TotalReceived`  | `long`   |                                      |
| `RequestHeaderLength` / `ResponseHeaderLength` | `int`    |                          |
| `ReusingConnection`            | `bool`   |                                      |
| `RequestProcessedOnConnection` | `int`    |                                      |
| `ErrorCount`                   | `int`    | `Exchange.Errors.Count`              |
| `Aborted`                      | `bool`   | Exchange aborted via rule            |
| `ClosedRemote`                 | `bool`   | Remote closed the connection         |

### 1009 `ConnectionEvicted` — Debug

Fired in `PoolBuilder.OnConnectionFaulted` when an upstream pool is removed
(today: H2 GoAway / fault).

| Property        | Type     | Meaning                                |
|-----------------|----------|----------------------------------------|
| `Authority`     | `string` |                                        |
| `Reason`        | `string` | Free-form (`PoolFaulted` today)        |
| `ConnectionId`  | `int?`   | H2 only (other pools null)             |

### 1010 `ConnectionOpened` — Debug

Fired in `RemoteConnectionBuilder.OpenConnectionToRemote` once a brand-new
upstream `Connection` is fully built.

| Property             | Type      | Meaning                                  |
|----------------------|-----------|------------------------------------------|
| `ConnectionId`       | `int`     |                                          |
| `Authority`          | `string`  |                                          |
| `RemoteIp` / `RemotePort` | `string` / `int` | Resolved upstream                |
| `LocalPort`          | `int`     | Bound local port                         |
| `HttpVersion`        | `string`  | `HTTP/1.1` / `HTTP/2`                    |
| `Alpn`               | `string?` |                                          |
| `TlsProtocol`        | `string?` |                                          |
| `CipherSuite`        | `string?` |                                          |
| `SniSent`            | `string?` |                                          |
| `TcpConnectMs`       | `double`  |                                          |
| `TlsHandshakeMs`     | `double`  |                                          |
| `ProxyConnectMs`     | `double`  | Time to negotiate upstream proxy CONNECT |
| `ViaUpstreamProxy`   | `bool`    |                                          |

### 1099 `ExchangeEnvelope` — Trace

Fired at exchange completion when `LogLevel.Trace` is enabled. One line carrying
the full request and response headers (and trailers when present).

| Property          | Type      | Meaning                                |
|-------------------|-----------|----------------------------------------|
| `ExchangeId`      | `int`     |                                        |
| `RequestHeaders`  | `string`  | `Name=Value`, one per line             |
| `ResponseHeaders` | `string?` | `Name=Value`, one per line             |
| `Trailers`        | `string?` | Response trailers when present         |

Header redaction is governed by two `FluxzySetting` properties:

- `LogIncludeSensitiveHeaders` (default `false`): when `false`, header values
  whose name appears in `LogRedactedHeaders` are replaced with
  `<redacted, len=N>`.
- `LogRedactedHeaders` (case-insensitive): default set is `Authorization`,
  `Proxy-Authorization`, `Cookie`, `Set-Cookie`, `X-Auth-Token`.

### 2001 `ClientConnectionInitFailed` — Warning

Fired when the inbound client-connection initialization fails (TLS handshake
from client failed, malformed CONNECT, etc.). Carries the exception and the
client `RemoteEndPoint`.

### 3001 `ConnectionProcessingError` — Error

Top-level catch in `Proxy.ProcessingConnection` — fires for unexpected errors
that aren't already handled by the orchestrator's per-exchange error paths.
Carries the exception and the client `RemoteEndPoint`.

## ActivitySource — `Fluxzy.Core`

One `Activity` is emitted per processed exchange (`ActivityKind.Server`,
operation name `"HTTP {Method}"`). Tags follow OpenTelemetry HTTP semantic
conventions where they map cleanly:

`http.request.method`, `url.full`, `server.address`, `server.port`,
`client.address`, `client.port`, `user_agent.original`,
`network.protocol.version`, `http.response.status_code`,
`http.request.body.size`, `http.response.body.size`.

Fluxzy-specific tags:

`fluxzy.exchange_id`, `fluxzy.pool.type`, `fluxzy.pool.reused`,
`fluxzy.dns.duration_ms`, `fluxzy.dns.forced`.

`ActivityStatusCode` is set to `Error` for 5xx / aborted / errored exchanges
and `Ok` for 2xx-3xx responses.

If the inbound request carries a W3C `traceparent` (and optional `tracestate`),
the Activity is started with that parent context so traces stitch end-to-end.
