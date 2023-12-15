![alt text](assets/full-logo.png "Title")


# fluxzy


[![build](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml/badge.svg)](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml)
[![gitter](https://badges.gitter.im/Fluxzy.svg)](https://app.gitter.im/#/room/!LRKtrkBMuIBYYNvHdA:gitter.im)
[![gitter](https://img.shields.io/badge/docs-latest-b36567)](https://docs.fluxzy.io/documentation/core/introduction.html)


fluxzy is a *fully managed* and *fully streamed* MITM engine and a CLI app to intercept, record and alter HTTP/1.1, H2, websocket traffic over plain or secure channels.

This repository contains the source code of [Fluxzy CLI](https://www.fluxzy.io/download#cli) which is a standalone command line application for Windows, macOS, and Linux and  the .NET packages that are used by [Fluxzy Desktop](https://www.fluxzy.io/download). 

## Stable version

### .NET packages

.NET packages target .NET Standard 2.1, .NET 6.0 and .NET 8.0.


| .NET Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core) |
| Fluxzy.Core.Pcap | Extensions for raw packet capture | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap) |

### Fluxzy CLI

| Fluxzy CLI | Version |
| --- | --- |
| Windows |   [![win32](https://fluxzy.io/misc/badge/cli/Windows32)  ![win64](https://fluxzy.io/misc/badge/cli/Windows64)      ![winArm64](https://fluxzy.io/misc/badge/cli/WindowsArm64)](https://www.fluxzy.io/download#cli)     | 
|macOS |  [![osx64](https://fluxzy.io/misc/badge/cli/Osx64)  ![osxArm64](https://fluxzy.io/misc/badge/cli/OsxArm64)](https://www.fluxzy.io/download#cli)   | 
| Linux |  [![linux64](https://fluxzy.io/misc/badge/cli/Linux64)  ![linuxArm64](https://fluxzy.io/misc/badge/cli/LinuxArm64)](https://www.fluxzy.io/download#cli)   |



## 1. Features

### 1.1 Core features 

- [x] Intercept HTTP/1.1, H2, WebSocket traffic over plain or secure channels
- [x] Fully streamed proxy
- [x] Deflect operating system traffic (act as system proxy)
- [x] Automatic root certificate installation (with elevation on Windows, macOS, and several Linux distributions) 
- [x] Certificate management: built-in feature to create CA compatible certificates 
- [x] Export as HTTP Archive (experimental)
- [x] Use a custom root certificate authority
- [x] Choice between default .NET SSL provider and BouncyCastle
- [x] [Raw packet capture](https://docs.fluxzy.io/documentation/core/04-capture-raw-packets.html) (with the extension `Fluxzy.Core.Pcap`)
- [x] NSS Key log extraction (when using Bouncy Castle)
- [x]  [Optional configuration-based data extraction and alteration](https://www.fluxzy.io/resources/documentation/the-rule-file) 

### 1.2 Alteration and traffic management features 

Alteration and traffic management features are available as [fluxzy actions](https://www.fluxzy.io/resources/documentation/core-concepts). You can browse this [dedicated search page](https://www.fluxzy.io/rule/find/) to see built-in actions on the latest stable version. Here are a few examples:

- [x] Add, remove, modify request and response headers
- [x] [Mock or substitute request and response body](https://docs.fluxzy.io/documentation/core/short-examples/mock-response.html)
- [x] Forward, redirect, spoof DNS, abort connections 
- [x] [Inject html snippet on intercepted request and response bodies](https://docs.fluxzy.io/documentation/core/short-examples/inject-code-in-html-pages.html)
- [x] [Remove cache directives](https://www.fluxzy.io/rule/item/removeCacheAction), add request and response cookies
- [x] [Serve static directory](https://www.fluxzy.io/rule/item/serveDirectoryAction) 
- [x] Add metadata to HTTP exchanges (tags and comments)
- [x] Provide a specific certificate for a host 
  

## 2. Basic Usage

### 2.1 Fluxzy CLI

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

### 2.2 .NET library

### 2.2.1 Simple usage
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
using Fluxzy.Rules.Actions;
using Fluxzy.Rules.Actions.HighLevelActions;
using Fluxzy.Rules.Filters;
using Fluxzy.Rules.Filters.RequestFilters;

// Create a new setting 
var fluxzySetting = FluxzySetting
    .CreateDefault(IPAddress.Loopback, 44344) // Listen on localhost:44344
    .SetOutDirectory("dump_directory"); // Save traffic to dump_directory

fluxzySetting
    .ConfigureRule() 
        // Forward request
        .WhenHostMatch("twitter.com", StringSelectorOperation.EndsWith) 
        .Forward("https://www.debunk.org/") 

        // Mock any POST request to /api/auth/token
        .WhenAll(new PostFilter(), new PathFilter("/api/auth/token"))
        .ReplyText("I lock the door and throw away the key", 403);

await using (var proxy = new Proxy(fluxzySetting))
{
    var endPoints = proxy.Run();

    var firstEndPoint = endPoints.First(); 

    Console.WriteLine($"Fluxzy is listen on the following endpoints: " +
                     $"{string.Join(" ", endPoints.Select(t => t.ToString()))}");

    // Create a test http sample matching fluxzy setting

    var httpClient = new HttpClient(new HttpClientHandler()
    {
        Proxy = new WebProxy(firstEndPoint.Address.ToString(), firstEndPoint.Port),
        ServerCertificateCustomValidationCallback = (_, _, _, _) => true
    }); 

    using var response = await httpClient.PostAsync("https://lunatic-on-the-grass.com/api/auth/token", null); 
    var responseText = await response.Content.ReadAsStringAsync();

    Console.WriteLine($"Final answer: {responseText}");

    Console.WriteLine("Press any key to exit...");
    Console.ReadLine();
}
```


Visit [docs.fluxzy.io](https://docs.fluxzy.io/documentation/core/introduction.html) to view detailed documentation and ready to use examples. 

## 3. Build

### 3.1 Requirements

- .NET 8.0 SDK
- Git bash for Windows
- `libpcap` or any equivalent library
- tests collecting pcap files and installing certificates requires elevation. 
- An IDE is not necessary to build the app. For information, this project was developed using both Visual Studio 2022 and JetBrains Rider on Windows, macOS and Linux.

### 3.2 Build

- Clone the repository
- Run  `dotnet build src/Fluxzy.Core` for Fluxzy.Core 
- Run  `dotnet build src/Fluxzy.Core.Pcap` for Fluxzy.Core.Pcap

### 3.3 Test 

- Several tests are run against various private web servers (iis, nginx, kestrel, apache, ...) which is not currently available to the public.


## 4 Contact 

- Use github issues for bug reports and feature requests
- [![gitter](https://badges.gitter.im/Fluxzy.svg)](https://app.gitter.im/#/room/!LRKtrkBMuIBYYNvHdA:gitter.im)
- Mail to **project@fluxzy.io** for inquiries