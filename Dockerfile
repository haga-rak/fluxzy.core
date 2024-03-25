FROM mcr.microsoft.com/dotnet/sdk:8.0 AS builder

WORKDIR /src

COPY ["", ""]

FROM builder AS publish
RUN dotnet publish "src/Fluxzy/fluxzy.csproj" -c Release --self-contained true --runtime linux-x64 /p:DebugType=None /p:DebugSymbols=false -o /app/output-bin

# APP IMAGE

# FROM alpine:3.19 AS base
FROM debian:bullseye-slim AS base

EXPOSE 44344

ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

# RUN apk add libpcap-dev

RUN apt update 
RUN apt install libpcap-dev -y

COPY --from=publish /app/output-bin /output

# CMD ["fluxzy", "start", "--container"]

CMD ["bash"]


