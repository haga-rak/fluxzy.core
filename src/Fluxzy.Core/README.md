# Fluxzy.Core

A [fast](https://fluxzy.io/resources/blogs/performance-benchmark-fluxzy-mitmproxy-mitmdump-squid), fully streamed MITM engine for intercepting, recording, and modifying HTTP/1.1, HTTP/2, WebSocket, and gRPC traffic over plain or TLS-secured channels.

This package is used under the hood by [Fluxzy Desktop](https://www.fluxzy.io/), a cross-platform HTTP debugger, and [Fluxzy CLI](https://github.com/haga-rak/fluxzy.core).

## Key Features

- Intercept HTTP/1.1, HTTP/2, WebSocket, and gRPC traffic
- Act as a system-wide proxy
- Export sessions as HTTP Archive (HAR) or Fluxzy Archive
- Choice of TLS providers: .NET native or BouncyCastle
- Impersonate JA4 fingerprints and custom HTTP/2 settings

## Traffic Modification

- Add, remove, or modify request and response headers
- Transform request and response bodies
- Mock or substitute request and response bodies
- Forward, redirect, spoof DNS, or abort connections
- Inject HTML snippets into responses
- Serve a static directory
- Provide custom TLS certificates per host

Browse all [built-in actions and filters](https://www.fluxzy.io/rule/find/).

## Supported Proxy Protocols

| Protocol | Description |
|----------|-------------|
| **HTTP CONNECT** | Standard forward proxy tunneling for HTTPS traffic |
| **SOCKS5** | Full SOCKS5 support with no-auth and username/password authentication |
| **Reverse Proxy** | Secure (TLS) and plain HTTP modes for backend service proxying |
| **System Proxy** | Transparent system-wide interception via OS proxy settings |

## Quick Start

```csharp
var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Any, 44344);

fluxzySetting
    .ConfigureRule()
    .WhenAny()
    .Do(new AddResponseHeaderAction("x-fluxzy", "Captured by Fluxzy"));

await using var proxy = new Proxy(fluxzySetting);
var endpoints = proxy.Run();

Console.WriteLine($"Fluxzy is running on {endpoints.First().Address}:{endpoints.First().Port}");
Console.ReadKey();
```

More examples in the [examples directory](https://github.com/haga-rak/fluxzy.core/tree/main/examples).

## Documentation

- [Full documentation](https://docs.fluxzy.io)
- [Rule file reference](https://www.fluxzy.io/resources/documentation/the-rule-file)
- [GitHub repository](https://github.com/haga-rak/fluxzy.core)
