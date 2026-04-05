# Fluxzy.Core.Pcap

An extension for [Fluxzy.Core](https://www.nuget.org/packages/Fluxzy.Core) that enables raw packet capture alongside HTTP(S) interception using [SharpPcap](https://github.com/dotpcap/sharppcap).

## What It Adds

- Capture and export deciphered raw packets in PCAP format
- Correlate raw TCP/TLS traffic with the corresponding HTTP exchanges
- NSS key log support for external analysis in Wireshark

## Requirements

A packet capture library must be installed on the host:
- **Linux/macOS**: libpcap
- **Windows**: Npcap

## Quick Start

```csharp
var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Any, 44344);

// Enable raw packet capture
fluxzySetting.SetCaptureRawPacket(true);

await using var proxy = new Proxy(fluxzySetting,
    new FluxzyNetOutOfProcessHost());  // uses pcap engine

var endpoints = proxy.Run();
```

## Documentation

- [Full documentation](https://docs.fluxzy.io)
- [Raw capture example](https://github.com/haga-rak/fluxzy.core/tree/main/examples/Samples.No003.RawCapture)
- [GitHub repository](https://github.com/haga-rak/fluxzy.core)
