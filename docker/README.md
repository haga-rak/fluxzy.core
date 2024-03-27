## What is Fluxzy ? 

Fluxzy is a fast and fully streamed Man-On-The-Middle engine and a CLI app to intercept, record and alter HTTP/1.1, H2, websocket traffic over plain or secure channels.

This docker image embedded Fluxzy CLI inside an alpine docker image. The entrypoint will land you directly to the `fluxzy` command line. 

> ⚠️ **Warning** 
> 
> Compare to the CLI, the only functional change you must consider is that the default start `fluxzy start` will listen to `0.0.0.0` instead of loopback. 


You can use this docker image to: 
- Test the CLI without installing in an controlled environment
- Use raw capture capabilities without elevated privileges or using it with `libpcap`
- Record a long running session


All command line options are available, however several settings can be set through environment variables for convenience.

Here are these environment variables:

| Variable | Description | Default |
|----------|-------------|---------|
| `FLUXZY_ADDRESS` | Address on which the server will run | `0.0.0.0` |
| `FLUXZY_PORT` | Port on which the server will run | `44344` |
| `FLUXZY_ENABLE_DUMP_FOLDER` | Dump captured requests to temp folder `/var/fluxzy/dump` | `FALSE` |
| `FLUXZY_ENABLE_OUTPUT_FILE` | Dump captured requests to `/var/fluxzy/out.fxzy` | `FALSE` |
| `FLUXZY_USE_BOUNCY_CASTLE` | Use Bouncy Castle AS SSL Provider | `FALSE` |
| `FLUXZY_CUSTOM_CA_PATH` | Custom CA cert path | Not defined |
| `FLUXZY_CUSTOM_CA_PASSWORD` | Custom CA cert password | Not defined |


## Github repository 

You can find the github repository for this docker image in the following link: [github.com/haga-rak/fluxzy.core](https://github.com/haga-rak/fluxzy.core).


## Usage with `docker run`

### Command line entry with basic usage

#### Get help message

```bash
docker run -it fluxzy/fluxzy:latest --help
```

#### Start on port `44344`  

The container will run and listen on 44344 port to handle incoming HTTP proxy requests. 

```bash	
docker run -it -p 43444:43444 fluxzy/fluxzy:latest start
```
Check the help message to know more about the options available.

## Usage with `docker-compose`

```yaml
version: '3'
services:
  fluxzy:
    environment:
      #  Instead of using the command line args, uncomment the following env var to control settings through environment variables
      # - FLUXZY_PORT=44344 # Port to listen on
      # - FLUXZY_ENABLE_OUTPUT_FILE=output_path  # output_path will contain the final .fxzy file after the process is halted
      # - FLUXZY_ENABLE_PCAP=1  # Enable PCAP capture
      # - FLUXZY_USE_BOUNCY_CASTLE=1  # Use Bouncy Castle for SSL
      # - FLUXZY_CUSTOM_CA_PATH=/var/fluxzy/cert.pfx  # Use PKCS12 file as custom CA
      # - FLUXZY_CUSTOM_CA_PASSWORD=password  # PKCS12 password
    image: fluxzy/fluxzy:latest        
    command: start # All default fluxzy CLI command are accepted here. 
    ports:
      - "44344:44344"

```