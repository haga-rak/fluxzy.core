
![alt text](assets/full-logo.png "Title")

# fluxzy 


fluxzy is a versatile HTTP intermediate and MITM engine for recording, analyzing, debugging, and altering HTTP/1.1, H2, WebSocket traffic over plain or secure channels.

This repository contains the .NET
 library and the [fluxzy cli](https://www.fluxzy.io/download#cli) that enables you to use fluxzy as a standalone application on a terminal.

| Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=netstandard2.1%20net6%20net8&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)|
| Fluxzy.Core.Pcap | Extensions that enables raw packet capture along the HTTP(S) exchange |  [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=netstandard2.1%20net6%20net8&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)|


## Features

### Core features 
- Capture raw packet along with HTTP requests (with the extension `Fluxzy.Core.Pcap`). NSS key log can be automatically retrieved when using Bouncy Castle
- Cross platform
- Deflect OS traffic (act as system proxy)
- Automatic certificate installation (with elevation on Windows, macOS and several linux distribution)
- Certificate management: build-in feature to create CA compatible certificate
- Export as Http Archive

### Alteration features 

#### Application level alteration features:
- Add, remove, modify request and response headers
- Change request method path, change status code and host
- Alter request and response body
- Mock request and response body
- Forward requests (reverse proxy like)
- Remove any cache directive
- Serve directory as static website
- Add request and response cookie
- Configuration-based data extraction and alteration
- Add metadas to HTTP exchanges (tags and comments)
- ......

#### Transport level alteration features
- Support HTTP/1.1, H2, WebSocket on outbound stream
- Spoof DNS
- Add client certificate
- Use a custom root certificate
- Use a specific certificate for host
- Force HTTP version
- Use specific TLS protocols
- Use native SSL client (SChannel, OpenSSL,...) or BouncyCastle
- Simulate transport failures
- ......


Check this [dedicated search page](https://www.fluxzy.io/rule/find/) to see all available directives. 

## Download and installation 

### NuGet packages

| Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=nuget&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)|
| Fluxzy.Core.Pcap | Extensions that enables raw packet capture along the HTTP(S) exchange |  [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=nuget&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)|


### CLI


Check [download page](https://www.fluxzy.io/download#cli) to see all available options.




To get started quickly, take a look at the [samples](https://github.com/haga-rak/fluxzy.core/tree/main/samples).


For more information, visit [fluxzy.io](https://fluxzy.io).
