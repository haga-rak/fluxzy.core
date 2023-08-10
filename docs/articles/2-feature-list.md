# Feature list 

The following feature list excludes all the alteration capabilities that can be found of the [rule search page](resources/search-rules). 

## Core feature

The following features are shared among any fluxzy products.  

##### HTTP traffic capture and storage

fluxzy can capture and store directly any HTTP exchanges into hard storage on the fly. The default output format is optimized for fast saving and minimize any performance overhead while storing HTTP Exchanges. Any HTTP trafic source can be captured as long with less or more extra configuration (include iOS and Android Application,.. )

##### Decode HTTPS session 

fluxzy use MITM to decode automatically any secure flow passing through the proxy.

##### Raw packet capture with NSS Keys

fluxzy can capture raw packet along with HTTP exchanges. Captured packets are bound to the corresponding HTTP exchange making easy to match packets composing an HTTP request and response.
NSSKeys are ephemeral keys created by the client and server during a SSL/TLS Negotiation in a specific format. Fluxzy can capture automatically NSS Keys when using a managed SSL/TLS Engine (based on BouncyCastle) instead of the native one (SChannel for Windows, OpenSSL for Linux,…). NSSKeys are directly injected to the outputed PCAPNG file to allow seamless plain text view on Wireshark or other PCAP reader. 

##### Codeless trafic alteration  

fluxzy uses rules from config files to alter or extract specific values from the HTTP trafic. 
Some example of alteration are : blocking, mocking request response, altering any header, forwarding, forcing cache removal, add client certificate, ...

The complete list of alteration capabilities can be found on the [rule search page](resources/search-rules). 



## Fluxzy Desktop features

##### Search everywhere 

Control any filters, any settings, any actions with a few keywords. 

##### Smart filtering

Choose and combine filter with few clicks. 

##### Replay  

Replay any previously captured HTTP exchange. 

##### Live edit

Halt, view, edit an continue any request passing through the proxy. 

##### Automatic system proxy 

Fluxzy can automatically update system proxy setting while starting a capture session. 




