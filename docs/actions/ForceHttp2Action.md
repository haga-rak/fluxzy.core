## forceHttp2Action

### Description

Forces the connection between fluxzy and remote to be HTTP/2.0. This value is enforced when setting up ALPN settings during SSL/TLS negotiation. <br/>The exchange will break if the remote does not support HTTP/2.0. <br/>This action will be ignored when the communication is clear (h2c not supported).

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

forceHttp2Action

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Forces the connection between fluxzy and remote to be HTTP/2.0. This value is enforced when setting up ALPN settings during SSL/TLS negotiation. <br/>The exchange will break if the remote does not support HTTP/2.0. <br/>This action will be ignored when the communication is clear (h2c not supported).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ForceHttp2Action
    noEditableSetting: true
```



### .NET reference

View definition of [ForceHttp2Action](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ForceHttp2Action.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [forceHttp11Action](forceHttp11Action)
 - [forceHttp2Action](forceHttp2Action)
 - [forceTlsVersionAction](forceTlsVersionAction)
 - [setClientCertificateAction](setClientCertificateAction)
 - [skipSslTunnelingAction](skipSslTunnelingAction)

