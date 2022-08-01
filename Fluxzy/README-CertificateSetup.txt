
openssl genrsa -des3 -out Fluxzy.key 4096
openssl req -x509 -new -nodes -key Fluxzy.key -sha256 -days 1024 -out Fluxzy.crt
openssl pkcs12 -in Fluxzy.crt -inkey Fluxzy.key -export -out Fluxzy.pfx


nuget.exe push -source \\192.168.1.95\rd\NugetPackages  bin/Debug/Fluxzy*

nuget add bin/Debug/Fluxzy* -source \\192.168.1.95\rd\NugetPackages