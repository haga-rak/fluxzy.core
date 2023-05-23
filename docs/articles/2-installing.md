# Installing

## Capturing raw packet 

You may need to perform additional operation in order to make fluxzy capture autmatically raw packet within HTTP exchanges. 

On windows : 

Install wincap on any equivalent capture library. fluxzy will automatically bound to the capture library to collect the raw packets. 

On Linux : 

libpcap is available by default on most distribution. 

On macOS : 

You can install libpcap through brew : 


## Installing the root certificate 


The default installation process should be seamless in any environment but we'd like to point out the privilege that fluxzy needs to acquire in order to perform quality capture.
To make fluxzy work at it's full potential, you need to add a certificate that fluxzy knows the private key. Per default, a default certificate will be suggest during the application installation. However, it's possible to specify your own certificate by using

  - environment variables : which indicates where fluxzy should get the root certificate

In any case, fluxzy should have access to the private key of the certificate to be able to decrypt trafic.
Depending whether or not you want trafic capture, you may need to acquire root priveleges (or admin on windows) to enable raw packet capture. For this again, fluxzy will ask you the root privilegies during the installation.
