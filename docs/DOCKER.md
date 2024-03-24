# Configure fluxzy docker image

Fluxzy docker settings can be changed through environment variables. The following table lists the available environment variables and their default values.

## Environment variables

| Variable | Description | Default |
|----------|-------------|---------|
| `FLUXZY_ADDRESS` | Address on which the server will run | `0.0.0.0` |
| `FLUXZY_PORT` | Port on which the server will run | `44344` |
| `FLUXZY_ENABLE_DUMP_FOLDER` | Dump captured requests to temp folder `/var/fluxzy/dump` | `FALSE` |
| `FLUXZY_ENABLE_OUTPUT_FILE` | Dump captured requests to `/var/fluxzy/out.fxzy` | `FALSE` |
| `FLUXZY_USE_BOUNCY_CASTLE` | Use Bouncy Castle AS SSL Provider | `FALSE` |
| `FLUXZY_CUSTOM_CA_PATH` | Custom CA cert path | Not defined |
| `FLUXZY_CUSTOM_CA_PASSWORD` | Custom CA cert password | Not defined |





## Example with docker run 

## Example with docker-compose

