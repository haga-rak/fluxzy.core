![alt text](assets/full-logo.png "Title")


# fluxzy


[![CI](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml/badge.svg)](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml)

fluxzy is an HTTP intermediate and MITM engine for recording and altering HTTP/1.1, H2, and WebSocket traffic over plain or secure channels.

This repository contains the source code of the .NET packages and [Fluxzy CLI](https://www.fluxzy.io/download#cli) that enables you to use fluxzy as a standalone application on a terminal on Windows, macOS, and Linux.

| .NET Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core) |
| Fluxzy.Core.Pcap | Extensions that enable raw packet capture along the HTTP(S) exchange | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=NuGet&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap) |


| Fluxzy CLI | Version |
| --- | --- |
| Windows |   [![win32](https://fluxzy.io/misc/badge/cli/Windows32)  ![win64](https://fluxzy.io/misc/badge/cli/Windows64)      ![winArm64](https://fluxzy.io/misc/badge/cli/WindowsArm64)](https://www.fluxzy.io/download#cli)     | 
|macOS |  [![osx64](https://fluxzy.io/misc/badge/cli/Osx64)  ![osxArm64](https://fluxzy.io/misc/badge/cli/OsxArm64)](https://www.fluxzy.io/download#cli)   | 
| Linux |  [![linux64](https://fluxzy.io/misc/badge/cli/Linux64)  ![linuxArm64](https://fluxzy.io/misc/badge/cli/LinuxArm64)](https://www.fluxzy.io/download#cli)   |





## 1. Features

To get an exhaustive list of all built-in alteration capabilities, check this [dedicated search page](https://www.fluxzy.io/rule/find/).

| Description | Category | Comment |
| --- | --- | --- |
| Deflect OS traffic (act as system proxy) | General | Partially on Linux |
| Automatic root certificate installation (with elevation on Windows, macOS, and several Linux distributions) | General | Partially on Linux |
| Certificate management: built-in feature to create CA compatible certificates | General ||
| Export as HTTP Archive | General ||
| View HTTP(s) traffic in clear text | General ||
| Capture raw packets along with HTTP requests (with the extension `Fluxzy.Core.Pcap`). NSS key log can be automatically retrieved when using Bouncy Castle | General ||
| Add, remove, modify request and response headers | Application-level alteration ||
| Change request method path, change status code, and host | Application-level alteration ||
| Mock request and response body | Application-level alteration ||
| [Forward requests (reverse proxy-like)](https://www.fluxzy.io/rule/item/forwardAction) | Application-level alteration ||
| [Remove any cache directives](https://www.fluxzy.io/rule/item/removeCacheAction) | Application-level alteration ||
| [Serve directory as a static website](https://www.fluxzy.io/rule/item/serveDirectoryAction) | Application-level alteration ||
| Add request and response cookies | Application-level alteration ||
| [Configuration-based data extraction and alteration](https://www.fluxzy.io/resources/documentation/the-rule-file) | Application-level alteration ||
| Add metadata to HTTP exchanges (tags and comments) | Application-level alteration ||
| Support HTTP/1.1, H2, WebSocket on outbound streams | Transport-level alteration ||
| Spoof DNS | Transport-level alteration ||
| [Add client certificates](https://www.fluxzy.io/rule/item/setClientCertificateAction) | Transport-level alteration ||
| Use a custom root certificate | Transport-level alteration ||
| [Use a specific certificate for hosts](https://www.fluxzy.io/rule/item/useCertificateAction) | Transport-level alteration | With native SSL engine |
| Force HTTP version | Transport-level alteration ||
| Use specific TLS protocols | Transport-level alteration ||
| Use native SSL client (SChannel, OpenSSL,...) or BouncyCastle | Transport-level alteration ||
| Abort connections | Transport-level alteration ||
| ...... | ...... ||



## 2. Basic Usage

### 2.1 Fluxzy CLI

The following demonstrates the basics of how to use fluxzy with a simple rule file.
The "rule file" is a straightforward YAML file containing a list of directives that fluxzy will evaluate during proxying.

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
A detail documentation is available at [docs.fluxzy.io](https://docs.fluxzy.io). 
The following shows a very basic usage of the .NET packages.

The main line to start a capture session is to create a [FluxzySetting](https://docs.fluxzy.io/documentation/core/fluxzy-settings.html) instance and use it to create a `Proxy` instance.

Install NuGet package `Fluxzy.Core` 

```bash
dotnet add package Fluxzy.Core
```
Run this top-level statement console app with .NET 6.0 or above, 

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
        .Forward("https://example.com") // Reply with the host content 

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

A more detailed documentation and samples are available at [fluxzy.io](https://www.fluxzy.io/resources/core/fluxzy-net-packages) or in the samples folder of this repository.