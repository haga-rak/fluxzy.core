
openssl genrsa -des3 -out Fluxzy.key 4096
openssl req -x509 -new -nodes -key Fluxzy.key -sha256 -days 1024 -out Fluxzy.crt
openssl pkcs12 -in Fluxzy.crt -inkey Fluxzy.key -export -out Fluxzy.pfx


# To capture without root 
sudo  setcap cap_net_raw=pe fxzy