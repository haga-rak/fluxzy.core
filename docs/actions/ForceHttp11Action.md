## forceHttp11Action

### Description

Force the connection between fluxzy and remote to be HTTP/1.1. This value is enforced by ALPN settings set during the SSL/Handshake handshake.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

forceHttp11Action

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Force the connection between fluxzy and remote to be HTTP/1.1. This value is enforced by ALPN settings set during the SSL/Handshake handshake.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ForceHttp11Action
```



### .NET reference

View definition of [ForceHttp11Action](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ForceHttp11Action.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [forceHttp11Action](forceHttp11Action)
 - [forceHttp2Action](forceHttp2Action)
 - [forceTlsVersionAction](forceTlsVersionAction)
 - [setClientCertificateAction](setClientCertificateAction)
 - [skipSslTunnelingAction](skipSslTunnelingAction)

