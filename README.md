
![alt text](assets/full-logo.png "Title")

# fluxzy 


fluxzy is a versatile HTTP intermediate and MITM engine for recording and altering HTTP/1.1, H2, WebSocket traffic over plain or secure channels.

This repository contains the .NET
 library and the [fluxzy CLI](https://www.fluxzy.io/download#cli) that enables you to use fluxzy as a standalone application on a terminal.

| Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=netstandard2.1%20net6%20net8&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)|
| Fluxzy.Core.Pcap | Extensions that enables raw packet capture along the HTTP(S) exchange |  [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=netstandard2.1%20net6%20net8&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)|


## Features

### Core features 
- Capture raw packet along with HTTP requests (with the extension `Fluxzy.Core.Pcap`). NSS key log can be automatically retrieved when using Bouncy Castle
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

Stable versions of fluxzy are available on NuGet.org.

| Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=netstandard2.1%20net6%20net8&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)|
| Fluxzy.Core.Pcap | Extensions that enables raw packet capture along the HTTP(S) exchange |  [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=netstandard2.1%20net6%20net8&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)|

### Fluxzy CLI


Check [download page](https://www.fluxzy.io/download#cli) to see all available options.


## Basic usage


### Fluxzy CLI 


The following shows basic was to use fluxzy. For a more detailed documentation, visit [fluxzy.io](https://www.fluxzy.io/resources/cli/overview) or just go with '--help' option available for each command.

#### Minimal start

```bash
fluxzy start
```

By default, this command line will start the proxy on port `127.0.0.1:44344`, which is equivalent to `fluxzy start -l 127.0.0.1:44344`

#### Force certificate registration when starting 

The option `--install-cert` will force the default certificate to be installed on the current user. This option needs elevation and may trigger interactive dialogs on certain OS. 
This option will do nothing if the certificate is already installed.

```bash
fluxzy start --install-cert
```

#### Act as system proxy 

Use the `-sp` option make fluxzy act as system proxy. The proxy settings will be reverted when fluxzy is stopped with SIGINT (Ctrl+C). The proxy settings won't be reverted if the fluxzy process is killed.

```bash
fluxzy start -sp
```


#### Save collected data

##### As a directory
The option `-d` will save all collected data in the specified directory. The directory will be created if it does not exist. 

```bash
fluxzy start -d ./dump_directory
```

##### As a fluxzy file 
The option `-o` will save all collected data in a fluxzy file. The file will be created only at the end of the capture session. 

We recommend using fxzy format as it supports holding PCAPNG files along with the HTTP exchanges 

```bash
fluxzy start -o data.fxzy
```

##### Export as HAR
The command `pack` contains feature to export collected data (directory) as HAR. 

```bash
fluxzy pack ./dump_directory -o data.har
```

#### Enable raw packet capture

The option `-c` will enable raw packet capture. This option may require elevated privileges on macOS and Linux. 

```bash
fluxzy start -d ./dump_directory -c
```

#### Read fxzy file 

The command `dissect` will read a fluxzy file and display all exchanges. 

```bash
fluxzy dissect data.fxzy
```

To search for a specific exchange, use the formation option `-f` with a `grep` or `find` pipe. For example, search all post request : 

```bash
fluxzy dis dd.fxzy -f "{id} - {method}" | grep POST
```

or on Windows

```bash
fluxzy dis dd.fxzy -f "{id} - {method}" | find "POST"
```



#### Extract a Pcap file from a fluxzy file. 

For example, the exchange id is `69`

```bash
fluxzy dis data.fxzy -i 69 -f "{pcap}" -o rawcapture.png
```










Specify `-d` option to start












To get started quickly, take a look at the [samples](https://github.com/haga-rak/fluxzy.core/tree/main/samples).


For more information, visit [fluxzy.io](https://fluxzy.io).
