# Overview 

Fluxzy is a multipurpose set of tool designed to provide detailed analysis and alteration of any kind of HTTP trafic. It takes advantage of the man of middle technics to enable capturing SSL/TLS streams in plain text and has an intuitive yaml based syntax to perform advance trafic filtering and alteration.

You can use fluxzy as :

  - **A desktop application**, allowing various recording and debugging task on most OS running Windows, macOS, Linux.
  - **A command line tool**, named fxzy, allowing trafic alteration, automation and other operation
  - **A .NET library** that can bring into your apps any features that fluxzy can do

# Get fluxzy

For Windows and macOS, you can get to the download pages to get the latest binaries for your OS.


# Installing
  
The default installation process should be seamless in any environment but we'd like to point out the privilege that fluxzy needs to acquire in order to perform quality capture.
To make fluxzy work at it's full potential, you need to add a certificate that fluxzy knows the private key. Per default, a default certificate will be suggest during the application installation. However, it's possible to specify your own certificate by using

  - environment variables : which indicates where fluxzy should get the root certificate

In any case, fluxzy should have access to the private key of the certificate to be able to decrypt trafic.
Depending whether or not you want trafic capture, you may need to acquire root priveleges (or admin on windows) to enable raw packet capture. For this again, fluxzy will ask you the root privilegies during the installation.
