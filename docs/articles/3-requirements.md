# Requirements

Fluxzy Desktop requires: 
 -  Windows 7 or later 
 -  macOS 10.15 or later 
 -  Gnome based Linux

## Enabling raw capture

To enable raw capture, you must install a capture library (libpcap, winpcap or any equivalent) which is not provided in the default installation. Fluxzy will detect automatically and use the capture engine. 

##### Debian based distribution

```bash 
sudo apt-get install libpcap-dev
```

##### Rpm based distribution

```bash 
dnf install libpcap-dev
```

####  On macOS : 

You can install libpcap through brew : 

```bash 
brew install libpcap
```

## Setup for particular devices

The desktop version has several wizards and auto configuration dialogs that is enough for common scenarios. 

However, to be able to capture particular remote devices or non-browser based application, you may need to perform extra configurations. These extra configuration are mainly:

 - Deflecting the trafic into the proxy : fluxzy uses the port 44344 per default
 - Trusting the root certificate : this is optional 

 Most HTTP clients support proxying, 

 - Setup an Android device
 - Setup an iOS device 
 - Setup Firefox Desktop 
 - Setup Firefox mobile 




