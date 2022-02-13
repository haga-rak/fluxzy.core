
openssl genrsa -des3 -out Echoes.key 4096
openssl req -x509 -new -nodes -key Echoes.key -sha256 -days 1024 -out Echoes.crt
openssl pkcs12 -in Echoes.crt -inkey Echoes.key -export -out Echoes.pfx


nuget.exe push -source \\192.168.1.95\rd\NugetPackages  bin/Debug/Echoes*

nuget add bin/Debug/Echoes* -source \\192.168.1.95\rd\NugetPackages