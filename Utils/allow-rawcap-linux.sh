#!/usr/bin/env bash

# NOTE: This will let anyone who belongs to the 'pcap' group
# execute 'dotnet' (for unit test purpose) 

sudo groupadd pcap
sudo usermod -a -G pcap $USER
sudo chgrp pcap /usr/share/dotnet/dotnet
sudo setcap cap_net_raw,cap_net_admin=eip /usr/share/dotnet/dotnet
