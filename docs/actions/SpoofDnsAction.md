## spoofDnsAction

### Description

Fix statically the remote ip or port disregards to the dns or host resolution of the current running system. Use this action to force the resolution of a hostname to a fixed IP address. 

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

spoofDnsAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Force the remote IP and port to be respectively 127.0.0.1 and 8080.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SpoofDnsAction
    remoteHostIp: 127.0.0.1
    remoteHostPort: 8080
```


Force the remote IP to be 127.0.0.1 (port remains the same as request by the client).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SpoofDnsAction
    remoteHostIp: 127.0.0.1
```


Force the remote port to be 8080 (IP remains the same as request by the client).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SpoofDnsAction
    remoteHostIp: 127.0.0.1
```



### See also

This action has no related action

