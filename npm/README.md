<div align="center">

![Fluxzy](https://raw.githubusercontent.com/haga-rak/fluxzy.core/main/assets/full-logo.png)

[![npm](https://img.shields.io/npm/v/@fluxzy/cli?label=npm&logo=npm)](https://www.npmjs.com/package/@fluxzy/cli)
[![build](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml/badge.svg)](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml)
[![GitHub](https://img.shields.io/github/license/haga-rak/fluxzy.core)](https://github.com/haga-rak/fluxzy.core/blob/main/LICENSE.md)

</div>

A [fast](https://fluxzy.io/resources/blogs/performance-benchmark-fluxzy-mitmproxy-mitmdump-squid) and fully streamed MITM proxy to intercept, record, and modify HTTP/1, HTTP/2, WebSocket, and gRPC traffic, whether in plain or secured with TLS.

Fluxzy is a man-in-the-middle (MITM) proxy that acts as both client and server, enabling interception and modification of HTTP traffic. It supports configuration-driven setups through rule files, allowing easy reuse and switching of configurations between CLI, .NET applications, and the Fluxzy Desktop application.

## Installation

```bash
npm install -g @fluxzy/cli
```

Or run directly without installing:

```bash
npx @fluxzy/cli start --llo
```

## Supported Platforms

| OS | Architectures |
|---|---|
| Windows | x64, x86, arm64 |
| macOS | x64, arm64 |
| Linux | x64, arm64 |

The correct platform binary is automatically selected at install time.

## Supported Proxy Protocols

| Protocol | Description |
|----------|-------------|
| **HTTP CONNECT** | Standard forward proxy tunneling for HTTPS traffic |
| **SOCKS5** | Full SOCKS5 support with no-auth and username/password authentication (RFC 1928) |
| **Reverse Proxy** | Secure (TLS) and plain HTTP modes for backend service proxying |
| **System Proxy** | Transparent system-wide interception via OS proxy settings |

Protocol detection is automatic: clients can connect using either HTTP CONNECT or SOCKS5 on the same listening port.

## Key Features

- Intercepts HTTP/1.1, HTTP/2, WebSocket, and gRPC traffic
- Captures and exports deciphered raw packets in PCAP format
- Exports sessions as HTTP Archive (HAR) or Fluxzy Archive
- Manages custom certificates
- Impersonates JA4 fingerprints and custom HTTP/2 settings
- Acts as a system-wide proxy

## Traffic Modification

- Add, remove, or modify request and response headers
- Transform request and response bodies
- Mock or substitute request and response bodies
- Forward, redirect, spoof DNS, block connections
- Inject HTML snippets into request and response bodies
- Remove cache directives, add request and response cookies
- Serve static directory
- Provide a specific TLS certificate for a given host

Browse the [directive search page](https://www.fluxzy.io/rule/find/) to see all built-in actions available.

## Quick Start

Start a basic proxy on localhost:

```bash
fluxzy start --llo
```

Test with curl:

```bash
curl -x 127.0.0.1:44344 https://www.fluxzy.io
```

## Using Rule Files

Rules are defined in YAML. Create a `rule.yaml` file:

```yaml
rules:
  - filter:
      typeKind: requestHeaderFilter
      headerName: authorization
      operation: regex
      pattern: "Bearer (?<BEARER_TOKEN>.*)"
    action:
      typeKind: FileAppendAction
      filename: token-file.txt
      text: "${authority.host} --> ${user.BEARER_TOKEN}\r\n"
      runScope: RequestHeaderReceivedFromClient
  - filter:
      typeKind: anyFilter
    action:
      typeKind: AddResponseHeaderAction
      headerName: fluxzy
      headerValue: Passed through fluxzy
```

Then start fluxzy with the rule file:

```bash
fluxzy start -r rule.yaml --install-cert -sp -o output.fxzy -c
```

| Option | Description |
|--------|-------------|
| `-r` | Load a rule file |
| `--install-cert` | Install the default certificate (requires elevation) |
| `-sp` | Register as system proxy |
| `-o` | Save captured traffic to a fluxzy archive |
| `-c` | Enable raw packet capture |

For more information about the rule syntax, visit the [documentation](https://www.fluxzy.io/resources/documentation/the-rule-file).

## Commands

```
Usage:
  fluxzy [command] [options]

Commands:
  start                                   Start a capturing session
  cert, certificate                       Manage root certificates
  pack <input-directory> <output-file>    Export to a specific archive format
  dis, dissect <input-file-or-directory>  Read a previously captured archive
```

Run `fluxzy start --help` for the full list of options.

## Other Installation Methods

Fluxzy CLI is also available through other package managers:

**Windows (winget):**
```bash
winget install Fluxzy.Fluxzy
```

**macOS (Homebrew):**
```bash
brew tap haga-rak/fluxzy
brew install fluxzy
```

**Docker:**
```bash
docker run -it -p 44344:44344 fluxzy/fluxzy:latest start
```

Binaries are also available on the [releases page](https://github.com/haga-rak/fluxzy.core/releases).

## Related

- [Fluxzy Desktop](https://www.fluxzy.io/download) - Cross-platform HTTP debugger GUI
- [Fluxzy.Core](https://www.nuget.org/packages/Fluxzy.Core) - .NET library (NuGet)
- [Documentation](https://docs.fluxzy.io)
- [GitHub](https://github.com/haga-rak/fluxzy.core)

## License

[GPL-3.0](https://github.com/haga-rak/fluxzy.core/blob/main/LICENSE.md)
