<div align="center">
    
![alt text](assets/full-logo.png "Title")

<hr/>

[![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=Fluxzy.Core&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)
[![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=Fluxzy.Core.Pcap&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)
[![Docker Image Version](https://img.shields.io/docker/v/fluxzy/fluxzy?label=docker&color=7155ab)](https://hub.docker.com/r/fluxzy/fluxzy)
[![build](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml/badge.svg)](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/haga-rak/fluxzy.core/graph/badge.svg?token=AD5R7Q1FHJ)](https://codecov.io/gh/haga-rak/fluxzy.core)
[![gitter](https://img.shields.io/badge/docs-latest-b36567)](https://docs.fluxzy.io/documentation/core/introduction.html)


[Features](#key-features) | [Quick usage (.NET)](#getting-started) | [Quick usage (CLI)](#sample-usage)  | [Quick usage (Docker)](#run-with-docker) | [Documentation](https://docs.fluxzy.io/documentation/core/introduction.html) | [Build](#3-build) | [License](LICENSE.md) | [Releases](https://github.com/haga-rak/fluxzy.core/releases)

</div>

fluxzy is a *fully managed* and *fully streamed* MITM engine and a CLI app to intercept, record and alter HTTP/1.1, H2, websocket traffic over plain or secure channels.

This repository contains the source code of [Fluxzy CLI](https://www.fluxzy.io/download#cli) which is a standalone command line application for Windows, macOS, and Linux and  the .NET packages that are used by [Fluxzy Desktop](https://www.fluxzy.io/download). 

## Key features

General features

- Intercept HTTP/1.1, H2, WebSocket traffic over plain or TLS
- Multiple mode: HTTP proxy, reverse proxy or transparent proxy
- [Record traffic as HTTP Archive or fxzy](https://docs.fluxzy.io/documentation/core/short-examples/export-http-archive-format.html)
- [Output PCAP and PCAPNG files](https://docs.fluxzy.io/documentation/core/04-capture-raw-packets.html) (with the extension `Fluxzy.Core.Pcap`)
- [Deflect operating system traffic (act as system proxy)](https://docs.fluxzy.io/documentation/core/06-capturing-os-trafic.html)
- [Custom root certificate authority](https://docs.fluxzy.io/documentation/core/short-examples/use-custom-root-certificate.html)
- [Automatic root certificate installation](https://docs.fluxzy.io/api/Fluxzy.FluxzySetting.html#Fluxzy_FluxzySetting_SetAutoInstallCertificate_System_Boolean_) (with elevation on Windows, macOS, and several Linux distributions) 
- [Certificate creation](https://www.fluxzy.io/resources/cli/command-cert): built-in feature to create CA compatible certificates 
- [Multiple TLS provider SChannel/OpenSSL/SecureTransport or BouncyCastle](https://docs.fluxzy.io/documentation/core/short-examples/export-http-archive-format.html)
- [NSS Key log extraction (when using Bouncy Castle)](https://docs.fluxzy.io/documentation/core/short-examples/export-http-archive-format.html)
- [Optional declarative traffic transformation](https://www.fluxzy.io/resources/documentation/the-rule-file) 

Alteration and traffic management features are available as [fluxzy actions](https://www.fluxzy.io/resources/documentation/core-concepts). You can browse this [dedicated search page](https://www.fluxzy.io/rule/find/) to see built-in actions on the latest stable version. Here are a few examples:

- Add, remove, modify request and response headers
- [Mock or substitute request and response body](https://docs.fluxzy.io/documentation/core/short-examples/mock-response.html)
- [Forward](https://www.fluxzy.io/rule/item/forwardAction), redirect, [spoof DNS](https://www.fluxzy.io/rule/item/spoofDnsAction), [abort connections](https://www.fluxzy.io/rule/item/abortAction) 
- [Inject html snippet on intercepted request and response bodies](https://docs.fluxzy.io/documentation/core/short-examples/inject-code-in-html-pages.html)
- [Remove cache directives](https://www.fluxzy.io/rule/item/removeCacheAction), add request and response cookies
- [Serve static directory](https://www.fluxzy.io/rule/item/serveDirectoryAction) 
- Add metadata to HTTP exchanges (tags and comments)
- [Provide a specific certificate for a host](https://www.fluxzy.io/rule/item/useCertificateAction)
  

## Getting started

### With .NET

The main documentation is available at [docs.fluxzy.io](https://docs.fluxzy.io). 
The following shows a very basic usage of the .NET packages.

The main line to begin a capture session is to create a [FluxzySetting](https://docs.fluxzy.io/documentation/core/fluxzy-settings.html) instance and use it to create a `Proxy` instance.

Install NuGet package `Fluxzy.Core` 

```bash
dotnet add package Fluxzy.Core
```
Create a top-level statement console app, with .NET 6.0 or above:

```csharp	
using System.Net;
using Fluxzy;
using Fluxzy.Core;
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;
using Fluxzy.Rules.Filters.ResponseFilters;

// Create a new setting 
var fluxzySetting = FluxzySetting.CreateDefault(IPAddress.Loopback, 8080);

fluxzySetting
    .ConfigureRule()
    // Forward request
    .WhenHostMatch("twitter.com")
    .Forward("https://www.google.com/")

    // Mock any POST request to /api/auth/token
    .WhenAll(
        new GetFilter(),
        new PathFilter("/api/auth/token", StringSelectorOperation.Contains))
    .ReplyJson("{ token: \"your fake key\" }")

    // Select wikipedia domains that produces text/html content-type
    .WhenAll(
        new HostFilter("wikipedia.[a-z]+$", StringSelectorOperation.Regex),
        new HtmlResponseFilter()
    )
    // Inject a CSS after opening head tag
    .Do(
        // Remove CSP to allow injecting CSS and scripts
        new DeleteResponseHeaderAction("Content-Security-Policy"),
        new InjectHtmlTagAction
        {
            Tag = "head",
            // Make all pages purple
            HtmlContent = "<style>* { background-color: #7155ab !important; }</style>"
        }
    );

await using var proxy = new Proxy(fluxzySetting);
var endPoints = proxy.Run();

// Register as system proxy, the proxy is restore when the IAsyncDisposable is disposed
await using var _ = await SystemProxyRegistrationHelper.Create(endPoints.First());

// Create a new HttpClient that uses the proxy 
var httpClient = HttpClientUtility.CreateHttpClient(endPoints, fluxzySetting);

var responseText = await httpClient.GetStringAsync("https://baddomain.com/api/auth/token");

Console.WriteLine($"Final answer: {responseText}");
Console.WriteLine("Press enter to halt this program and restore system proxy setting...");

Console.ReadLine();
```


More examples are available at [docs.fluxzy.io](https://docs.fluxzy.io/documentation/core/introduction.html).


## As a command line tool

| Fluxzy CLI | Version |
| --- | --- |
| Windows |   [![win32](https://fluxzy.io/misc/badge/cli/Windows32)  ![win64](https://fluxzy.io/misc/badge/cli/Windows64)      ![winArm64](https://fluxzy.io/misc/badge/cli/WindowsArm64)](https://www.fluxzy.io/download#cli)     | 
|macOS |  [![osx64](https://fluxzy.io/misc/badge/cli/Osx64)  ![osxArm64](https://fluxzy.io/misc/badge/cli/OsxArm64)](https://www.fluxzy.io/download#cli)   | 
| Linux |  [![linux64](https://fluxzy.io/misc/badge/cli/Linux64)  ![linuxArm64](https://fluxzy.io/misc/badge/cli/LinuxArm64)](https://www.fluxzy.io/download#cli)   |

### Sample usage

The following highlights the basic way to use fluxzy with a simple rule file.

The ["rule file"](https://www.fluxzy.io/resources/documentation/the-rule-file) is a straightforward YAML file containing a list of directives that fluxzy will evaluate during proxying.

For more detailed documentation, visit [fluxzy.io](https://www.fluxzy.io/resources/cli/overview) or use the `--help` option available for each command.

Create a `rule.yaml` file as follows:

```yaml
rules:
  - filter:
      typeKind: requestHeaderFilter
      headerName: authorization # Select only requests with authorization header
      operation: regex
      pattern: "Bearer (?<BEARER_TOKEN>.*)" # A named regex instructs fluxzy
                                             # to extract the token from the authorization
                                             # header into the variable BEARER_TOKEN
    action:
      # Write the token to a file
      typeKind: FileAppendAction # Append the token to the file
      filename: token-file.txt # Save the token to token-file.txt
      text: "${authority.host} --> ${user.BEARER_TOKEN}\r\n"  # user.BEARER_TOKEN retrieves 
                                                              # the previously captured variable 
      runScope: RequestHeaderReceivedFromClient  # Run the action when the request header 
                                                 # is received from the client
  - filter:
      typeKind: anyFilter # Apply to any exchanges
    action:
      typeKind: AddResponseHeaderAction # Append a response header
      headerName: fluxzy
      headerValue: Passed through fluxzy 

```

The rule file above performs two actions:
  - It extract any BEARER token from the authorization header and write it to a file (`token-file.txt``)
  - It appends a response header (`fluxzy: Passed through fluxzy`) to all exchanges

For more information about the rule syntax, visit the [documentation](https://www.fluxzy.io/resources/documentation/the-rule-file) page.
Visit [directive search page](https://www.fluxzy.io/rule/find) to see all built-in filters and actions.


Then start fluxzy with the rule file

```bash
fluxzy start -r rule.yaml --install-cert -sp -o output.fxzy -c 
```

- `--install-cert`, `-sp`, `-o`, `-c`, `-r` are optional.

- `-o` will save all collected data in a fluxzy file. The file will be created only at the end of the capture session. 

- `-sp` will make fluxzy act as system proxy. The proxy settings will be reverted when fluxzy is stopped with SIGINT (Ctrl+C). The proxy settings won't be reverted if the fluxzy process is killed.

- `-c` will enable raw packet capture.

- `--install-cert` will install the default certificate on the current user. This option needs elevation and may trigger interactive dialogs on certain OS.

You can use the command [`dissect`](https://www.fluxzy.io/resources/cli/command-dissect) to read the fluxzy file or, alternatively, you can use [Fluxzy Desktop](https://www.fluxzy.io/download) to view it with a GUI. 

More command and options are available, including [exporting to HAR](https://www.fluxzy.io/resources/cli/command-pack#pack-a-fluxzy-dump-directory-into-a-fluxzy-archive) or [managing certificates](https://www.fluxzy.io/resources/cli/command-cert), you can run `--help` to see all available options and commands.

By default, fluxzy will bind to `127.0.0.1:44344`.

### Run with docker

The CLI can be run from a [docker image](https://hub.docker.com/r/fluxzy/fluxzy).

```bash
docker run -it -p 43444:43444 fluxzy/fluxzy:latest start
```

To test: 

```bash
curl -x 127.0.0.1:44344 https://www.fluxzy.io
```

## Build

### Requirements

- .NET 8.0 SDK
- Git bash if Windows
- `libpcap` or any equivalent library
- tests collecting pcap files and installing certificates requires elevation. 
- An IDE is not necessary to build the app. For information, this project was developed using both Visual Studio 2022 and JetBrains Rider on Windows, macOS and Linux.

### Actual Build

- Clone the repository
- Run  `dotnet build src/Fluxzy.Core` for Fluxzy.Core 
- Run  `dotnet build src/Fluxzy.Core.Pcap` for Fluxzy.Core.Pcap

### Testing 

- Several tests are run against various private web servers (iis, nginx, kestrel, apache, ...) which is not currently available to the public.


## Contact 

- Use github issues for bug reports and feature requests
- Mail to **project@fluxzy.io** for inquiries
