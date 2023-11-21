
![alt text](assets/full-logo.png "Title")

# fluxzy 

[![CI](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml/badge.svg)](https://github.com/haga-rak/fluxzy.core/actions/workflows/ci.yml)

fluxzy is an HTTP intermediate and MITM engine for recording and altering HTTP/1.1, H2, WebSocket traffic over plain or secure channels.

This repository contains the .NET
 library and  [Fluxzy CLI](https://www.fluxzy.io/download#cli) that enables you to use fluxzy as a standalone application on a terminal on Windows, macOS  and linux.

| Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=nuget&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)|
| Fluxzy.Core.Pcap | Extensions that enables raw packet capture along the HTTP(S) exchange |  [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=nuget&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)|


## 1. Features

| Description | Category | Comment | 
| --- | --- | --- |
| Deflect OS traffic (act as system proxy) | General | Partially on linux|
| Automatic root certificate installation (with elevation on Windows, macOS and several linux distribution) | General |Partially on linux|
| Certificate management: build-in feature to create CA compatible certificate | General ||
| Export as Http Archive | General ||
| View HTTP(s) traffic in clear text | General ||
| Capture raw packet along with HTTP requests (with the extension `Fluxzy.Core.Pcap`). NSS key log can be automatically retrieved when using Bouncy Castle | General ||
| Add, remove, modify request and response header | Application level alteration ||
| Change request method path, change status code and host | Application level alteration ||
| Mock request and response body | Application level alteration ||
| Forward requests (reverse proxy like) | Application level alteration ||
| Remove any cache directive | Application level alteration ||
| Serve directory as static website | Application level alteration ||
| Add request and response cookie | Application level alteration ||
| Configuration-based data extraction and alteration | Application level alteration ||
| Add metadas to HTTP exchanges (tags and comments) | Application level alteration ||
| Support HTTP/1.1, H2, WebSocket on outbound stream | Transport level alteration ||
| Spoof DNS | Transport level alteration ||
| Add client certificate | Transport level alteration ||
| Use a custom root certificate | Transport level alteration ||
| Use a specific certificate for host | Transport level alteration |With native SSL engine|
| Force HTTP version | Transport level alteration ||
| Use specific TLS protocols | Transport level alteration ||
| Use native SSL client (SChannel, OpenSSL,...) or BouncyCastle | Transport level alteration ||
| Abort connection | Transport level alteration ||
| ...... | ...... ||



Check this [dedicated search page](https://www.fluxzy.io/rule/find/) to see all available directives. 

## 2. Download and installation 

### 2.1 NuGet packages

Stable and signed versions of **fluxzy** are available on NuGet.org.

| Package | Description | Version |
| --- | --- | --- |
| Fluxzy Core | Core library | [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=nuget&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core)|
| Fluxzy.Core.Pcap | Extensions that enables raw packet capture along the HTTP(S) exchange |  [![Fluxzy.Core](https://img.shields.io/nuget/v/Fluxzy.Core.svg?label=nuget&logo=nuget)](https://www.nuget.org/packages/Fluxzy.Core.Pcap)|

### 2.2 Fluxzy CLI


Check [download page](https://www.fluxzy.io/download#cli) to see all available options.


## 3. Basic usage


### 3.1 Fluxzy CLI 

The following shows basic of how to use fluxzy with a simple rule file. For a more detailed documentation, visit [fluxzy.io](https://www.fluxzy.io/resources/cli/overview) or just go with `--help` option available for each command.


Create a `rule.yaml` file as the following


```yaml	
rules:
  - filter: 
      typeKind: requestHeaderFilter
      headerName: authorization # select only request with authorization header
      operation: regex
      pattern: "Bearer (?<BEARER_TOKEN>.*)" # A named regex instructs fluxzy
                                             # to extract token from authorization
                                             # header into the variable BEARER_TOKEN
    action : 
      # Write the token on file 
      typeKind: FileAppendAction # Append the token to the file
      filename: token-file.txt # save the token to token-file.txt
      text: "${authority.host} --> ${user.BEARER_TOKEN}\r\n"  # user.BEARER_TOKEN retrieves 
                                            # the previously captured variables 
      runScope: RequestHeaderReceivedFromClient  # run the action when the request header 
                                                 # is received from clientrules:
  - filter: 
      typeKind: anyFilter # Apply to any exchanges
    action :  
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

`--install-cert`, `-sp`, `-o`, `-c`, `-r` are optional.

`-o` will save all collected data in a fluxzy file. The file will be created only at the end of the capture session. 

`-sp` will make fluxzy act as system proxy. The proxy settings will be reverted when fluxzy is stopped with SIGINT (Ctrl+C). The proxy settings won't be reverted if the fluxzy process is killed.

`-c` will enable raw packet capture.

`--install-cert` will install the default certificate on the current user. This option needs elevation and may trigger interactive dialogs on certain OS.

You can use the command `dissect` to read the fluxzy file or, alternatively, use [Fluxzy Desktop](https://www.fluxzy.io/download) to view it with a GUI. 


