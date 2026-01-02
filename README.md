<div align="center">
    
![alt text](assets/full-logo.png "Title")

<hr/>

[![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=Fluxzy.Core&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)
[![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=Fluxzy.Core.Pcap&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)
[![Docker Image Version](https://img.shields.io/docker/v/fluxzy/fluxzy?label=docker&color=7155ab)](https://hub.docker.com/r/fluxzy/fluxzy)
[![build](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml/badge.svg)](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/haga-rak/fluxzy.core/graph/badge.svg?token=AD5R7Q1FHJ)](https://codecov.io/gh/haga-rak/fluxzy.core)
[![gitter](https://img.shields.io/badge/docs-latest-b36567)](https://docs.fluxzy.io/documentation/core/introduction.html)


[Features](#-key-features) | [Quick usage (.NET)](#-usage) | [Quick usage (CLI)](#using-cli)  | [Quick usage (Docker)](#using-docker-container) | [Documentation](https://docs.fluxzy.io/documentation/core/introduction.html) | [Build](#-build) | [License](LICENSE.md) | [Releases](https://github.com/haga-rak/fluxzy.core/releases)

</div>

<i>A [fast](https://fluxzy.io/resources/blogs/performance-benchmark-fluxzy-mitmproxy-mitmdump-squid) and fully streamed MITM tool to intercept, record, and modify HTTP/1, HTTP/2, and WebSocket traffic, whether in plain or secured with TLS.</i>

Fluxzy is a man-in-the-middle (MITM) proxy that acts as both client and server, enabling interception and modification of HTTP/1, HTTP/2, and WebSocket traffic. It’s designed for high performance, using full streaming to minimize overhead, while response bodies exceeding the initial buffer are fully stored in memory for inspection. Fluxzy supports configuration-driven setups through rule files, allowing easy reuse and switching of configurations between CLI, .NET applications, and the Fluxzy Desktop application without additional effort.

Fluxzy can be used as a CLI tool, a Docker container or a .NET library and is used under the hood by [Fluxzy Desktop](https://www.fluxzy.io/download) which is a cross-platform HTTP debugger.

## ⚙️ Key Features

- [Intercepts HTTP/1.1, HTTP/2, and WebSocket traffic](examples/Samples.No004.BasicAlterations/Program.cs)  
- [Acts as a system-wide proxy](examples/Samples.No006.CaptureOsTraffic/Program.cs)  
- [Captures and exports deciphered raw packets in PCAP format](examples/Samples.No003.RawCapture/Program.cs)  
- [Offers a choice of TLS providers: .NET native or BouncyCastle](https://docs.fluxzy.io/api/Fluxzy.FluxzySetting.html#Fluxzy_FluxzySetting_UseBouncyCastleSslEngine)
- [Exports sessions as HTTP Archive (HAR) or Fluxzy Archive](examples/Samples.No001.RecordAsHarOrFxzy/Program.cs)  
- [Manages custom certificates](https://www.fluxzy.io/resources/cli/command-cert)  
- [Impersonates JA4 fingerprints and custom HTTP/2 settings](examples/Samples.No016.ImpersonateBrowser/Program.cs)

## 🧪 Key Traffic Modification Features

- [Add, remove, or modify request and response headers](examples/Samples.No013.ModifyHeaders/Program.cs)  
- [Transform request](examples/Samples.No017.TransformRequestBody/Program.cs) and [transform response](examples/Samples.No018.TransformResponseBody/Program.cs) bodies from the original content  
- [Mock or substitute request and response bodies](examples/Samples.No010.MockResponse/Program.cs) 
- [Forward](https://www.fluxzy.io/rule/item/forwardAction), redirect, [spoof DNS](https://www.fluxzy.io/rule/item/spoofDnsAction), [block or connections](https://www.fluxzy.io/rule/item/abortAction) 
- [Inject HTML snippets into request and response bodies](examples/Samples.No009.InjectCodeSnippet/Program.cs)  
- [Remove cache directives](https://www.fluxzy.io/rule/item/removeCacheAction), add [request](examples/Samples.No011.AddRequestCookie/Program.cs) and [response](examples/Samples.No012.AddResponseCookie/Program.cs) cookies
- [Serve static directory](https://www.fluxzy.io/rule/item/serveDirectoryAction) 
- [Provide a specific TLS certificate for a given host](https://fluxzy.io/rule/item/setClientCertificateAction)
- [Provide a specific certificate for a host](https://www.fluxzy.io/rule/item/useCertificateAction)

You can browse this [dedicated search page](https://www.fluxzy.io/rule/find/) to see all built-in actions available.


## 📘 Usage

### Integrate with a .NET application

Install NuGet package `Fluxzy.Core` 

```bash
dotnet add package Fluxzy.Core
```
Create a top-level statement console app, with .NET 10.0 or above:

```csharp	
// Creating settings
var fluxzySetting = FluxzySetting
    .CreateDefault(IPAddress.Any, 44344);

// configure rules
fluxzySetting
    .ConfigureRule()
    .WhenAny()
    .Do(new AddResponseHeaderAction("x-fluxzy", "Captured by Fluxzy"))
    .WhenAll(new JsonResponseFilter(), new StatusCodeSuccessFilter())
    .Do(new MockedResponseAction(MockedResponseContent.CreateFromPlainText("Not allowed to return JSON", 403, "text/plain")));

// Create proxy instance and run it
await using var proxy = new Proxy(fluxzySetting);
var endpoints = proxy.Run();

Console.WriteLine($"Fluxzy is running on {endpoints.First().Address}:{endpoints.First().Port}");
Console.WriteLine("Press any key to stop the proxy and exit...");
Console.ReadKey();
```

More use cases are available in [examples directory](./examples/). The main documentation is available at [docs.fluxzy.io](https://docs.fluxzy.io). 


### Using the CLI

| Fluxzy CLI | Version |
| --- | --- |
| Windows |   [![win32](https://fluxzy.io/misc/badge/cli/Windows32)  ![win64](https://fluxzy.io/misc/badge/cli/Windows64)      ![winArm64](https://fluxzy.io/misc/badge/cli/WindowsArm64)](https://www.fluxzy.io/download#cli)     | 
|macOS |  [![osx64](https://fluxzy.io/misc/badge/cli/Osx64)  ![osxArm64](https://fluxzy.io/misc/badge/cli/OsxArm64)](https://www.fluxzy.io/download#cli)   | 
| Linux |  [![linux64](https://fluxzy.io/misc/badge/cli/Linux64)  ![linuxArm64](https://fluxzy.io/misc/badge/cli/LinuxArm64)](https://www.fluxzy.io/download#cli)   |

#### CLI Installation

**Windows (winget):**
```bash
winget install Fluxzy.Fluxzy
```

**macOS (Homebrew):**
```bash
brew tap haga-rak/fluxzy
brew install fluxzy
```

Alternatively, you can download the binaries directly from the [releases page](https://github.com/haga-rak/fluxzy.core/releases) or [fluxzy.io](https://www.fluxzy.io/download#cli).

<details>
    <summary><code>fluxzy</code> root commands</summary>
    
```bash
Usage:
  fluxzy [command] [options]

Options:
  -v, --version   Show version information
  -?, -h, --help  Show help and usage information

Commands:
  start                                   Start a capturing session
  cert, certificate                       Manage root certificates used by the fluxzy
  pack <input-directory> <output-file>    Export a fluxzy result directory to a specific archive format
  dis, dissect <input-file-or-directory>  Read content of a previously captured archive file or directory.
```  
</details>

<details>
    <summary><code>fluxzy start</code> options </summary>
    
```bash
Usage:
  fluxzy start [options]

Options:
  -l, --listen-interface <listen-interface>    Set up the binding addresses. Default value is "127.0.0.1:44344" which
                                               will listen to localhost on port 44344. 0.0.0.0 to listen on all
                                               interface with the default port. Use port 0 to let OS assign a random
                                               available port. Accepts multiple values. [default: 127.0.0.1:44344]
  --llo                                        Listen on localhost address with default port. Same as -l
                                               127.0.0.1/44344 [default: False]
  --lany                                       Listen on all interfaces with default port (44344) [default: False]
  -o, --output-file <output-file>              Output the captured traffic to an archive file []
  -d, --dump-folder <dump-folder>              Output the captured traffic to folder
  -r, --rule-file <rule-file>                  Use a fluxzy rule file. See more at :
                                               https://www.fluxzy.io/resources/documentation/the-rule-file
  -sp, --system-proxy                          Try to register fluxzy as system proxy when started [default: False]
  -b, --bouncy-castle                          Use Bouncy Castle as SSL/TLS provider [default: False]
  -c, --include-dump                           Include tcp dumps on captured output [default: False]
  -ss, --skip-ssl-decryption                   Disable ssl traffic decryption [default: False]
  -t, --trace                                  Output trace on stdout [default: False]
  -i, --install-cert                           Install root CA in current cert store if absent (require higher
                                               privilege) [default: False]
  --no-cert-cache                              Don't cache generated certificate on file system [default: False]
  --cert-file <cert-file>                      Substitute the default CA certificate with a compatible PKCS#12 (p12,
                                               pfx) root CA certificate for SSL decryption
  --cert-password <cert-password>              Set the password of certfile if any
  -R, --rule-stdin                             Read rule from stdin
  --parse-ua                                   Parse user agent [default: False]
  --use-502                                    Use 502 status code for upstream error instead of 528. [default: False]
  --external-capture                           Indicates that the raw capture will be done by an external process
                                               [default: False]
  --mode <Regular|ReversePlain|ReverseSecure>  Set proxy mode [default: Regular]
  --mode-reverse-port <mode-reverse-port>      Set the remote authority port when --mode ReverseSecure or --mode
                                               ReversePlain is set []
  --proxy-auth-basic <proxy-auth-basic>        Require a basic authentication. Username and password shall be provided
                                               in this format: username:password. Values can be provided in a percent
                                               encoded format. []
  --request-buffer <request-buffer>            Set the default request buffer []
  -n, --max-capture-count <max-capture-count>  Exit after a specified count of exchanges []
  -?, -h, --help                               Show help and usage information
```  
</details>



<details>
    <summary>Usage overview </summary>

The following highlights the basic way to use fluxzy with an optional rule file.
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

</details>

### Using docker container

The CLI can be run from a [docker image](https://hub.docker.com/r/fluxzy/fluxzy).

```bash
docker run -it -p 44344:44344 fluxzy/fluxzy:latest start
```

To test: 

```bash
curl -x 127.0.0.1:44344 https://www.fluxzy.io
```

## 🏗️ Build

### Requirements

- .NET 10.0 SDK  
- Git Bash (required on Windows)  
- `libpcap` or an equivalent packet capture library (tests that collect PCAP files or install certificates require elevated privileges)  
- No IDE is required to build the application. For reference, the project has been developed and tested using Visual Studio 2022 and JetBrains Rider on Windows, macOS, and Linux

### Building the Project

To build the core components:

```bash
# Clone the repository
git clone https://github.com/haga-rak/fluxzy.core.git
cd fluxzy.core

# Build the core library
dotnet build src/Fluxzy.Core

# Build the packet capture extension
dotnet build src/Fluxzy.Core.Pcap
```


## 📬 Contact 

- Use github issues for bug reports and feature requests
- Mail to **project@fluxzy.io** for inquiries
