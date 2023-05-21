## forceTlsVersionAction

### Description

Force the usage of a specific TLS version. Values can be chosen among : Tls, Tls11, Tls12, Tls13, Ssl3, Ssl2. <br/>Forcing the usage of a specific TLS version can break the exchange if the remote does not support the requested protocol.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    forceTlsVersionAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| sslProtocols | none \| ssl2 \| ssl3 \| tls \| default \| tls11 \| tls12 \| tls13 |  | none |

### Example of usage

The following examples apply this action to any exchanges

Accept only TLS 1.1 connections.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ForceTlsVersionAction
    sslProtocols: Tls11
```



