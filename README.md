

## fluxzy 
Fluxzy is a HTTP analyzer engine, focused on performance, build with .NET. 
This project aims to give you a complete environment to understand and debug any issue around HTTP.

## fluxzy.cli (fxzy) 
fxzy is a command line tool taking full advantage of the fluxzy engine, allowing you to a automate fluxzy features without writing any line of code. Run `fxzy --help` to get more information about this tool. 

## fluxzy.desktop
fluxzy.desktop is a desktop application taking full advandage the fluxzy engine. Build with electron/angular/.NET, it runs on major platform supporting this later. 

## Features list 
Find below the list of features that is currently implemented : 
 - **SSL/TLS decryption** : fluxzy performs man on the middle to enable viewing HTTP(S) sessions in plain text. By default, fluxzy uses a default embedded certificate but using a custom certificate is supported.
 - **H2 on egress** : fluxzy support H2 protocol on egress. This means that ingress HTTP/1.1 requests can be translated into H2 as long as server supports it. fluxzy uses a custom implementation of the H2 protocol, thus, customizing low level H2 setting is possible (window size algorithm, dynamic table size, ...)
 - **Powerful rules filter** : the filter system of fluxzy is designed to give the most flexibility for end user. Alterations goes from spoofing DNS IP to complete, conditional rewrite of a response. 
 - **Always streaming** : fluxzy was designed to have the less overhead possible to minimize impact of performance measure. 
 
 ### Current filters available in fluxzy.desktop
 - By host, by authority
 - By IP address
 - By request, method, path, full url 
 - By any request header
 - By any response header
 - By cookie 
 
 ### Currently available alterations
 - Spoofing DNS
 - Remapping hostname
 - Disabling decryption 
 - Adding client certificates 
 - Replacing request method, path
 - Replacing, removing or adding new requests headers
 - Replacing request body
 - Replacing response status 
 - Replacing response body 
 
 
 
