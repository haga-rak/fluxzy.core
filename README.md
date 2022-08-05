# fluxzy 
fluxzy is an HTTP analyzer and debugger engine, designed for delivering high end diagnosis and accurate performance measures. fluxzy targets .NET Standard 2.1, that means that it runs on any platform supporting this later. 

# fluxzy.cli (fxzy) 
fxzy is a command line tool taking full advantage of the fluxzy engine, allowing you to a automate fluxzy features without writing any line of code. Run `fxzy --help` to get more information about this tool. 

# fluxzy.desktop
fluxzy.desktop is a desktop application build with electron/angular/.NET. It is available on major platform (macOS, linux, Windows) and aims to be a strong tool for analyzing and debugging HTTP traffic.

### How does fluxzy works? 
Fluxzy is an HTTP proxy like many other tool on market. It takes advantages of the man on the middle technics to enable viewing and debuging secure HTTP streams. However, under the hood, fluxzy is quiet different from the common market HTTP debugger for not being a simple combination of an HTTP client and an HTTP server. 
In fact fluxzy was build with a custom HTTP client / server and engine allowing the lowest level of passive behavior. 

## Features list 
Find below the list of features that are currently supported : 
 - **SSL/TLS decryption** : fluxzy performs man on the middle to enable viewing HTTP(S) sessions in plain text. By default, fluxzy uses a default embedded certificate.  Using a custom certificate is also supported.
 - **H2 on egress** : fluxzy support H2 protocol on egress. This means that ingress HTTP/1.1 requests can be translated into H2 as long as server supports it. fluxzy uses a custom implementation of the H2 protocol, thus, customizing low level H2 setting is possible (window size algorithm, dynamic table size, ...)
 - **Powerful rules filter** : fluxzy uses an unique and powerfull filter system to manipulate and alter the traffic.
 - **Always streaming** : fluxzy was designed to produced a minimal overhead in order to deliver the most accurate performance measurement. 
 
 ### Current filters available in fluxzy.desktop
 - By host, by authority
 - By IP address
 - By request, method, path, full url 
 - By any request header
 - By any response header
 - By cookie 
 
### Currently available alterations
Alterations are active trafic modification operated by the fluxzy engine. 

#### Low level alterations
 - Spoofing DNS
 - Remapping hostname
 - Disabling decryption 
 - Adding client certificates 
 - Replacing request method, path
 - Replacing, removing or adding new requests headers
 - Replacing request body
 - Replacing response status 
 - Replacing response body 
 
### High level alterations :
High level alterations are build around low level alterations, and aspires to give a better accessibility to commonly used alteration. 

 - Replacing, removing adding cookie 
 - Response with a premade response 
 - Remove caches
 - Build caches (see tutorial) 
 - Changing user agent 
 
 
 
