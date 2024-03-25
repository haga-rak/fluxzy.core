## skipSslTunnelingAction

### Description

Instructs fluxzy to not decrypt the current traffic. The associated filter  must be on OnAuthorityReceived scope in order to make this action effective. 

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

skipSslTunnelingAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Instructs fluxzy to not decrypt the current traffic. The associated filter  must be on OnAuthorityReceived scope in order to make this action effective.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SkipSslTunnelingAction
```



### .NET reference

View definition of [SkipSslTunnelingAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.SkipSslTunnelingAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [forceHttp11Action](forceHttp11Action)
 - [forceHttp2Action](forceHttp2Action)
 - [forceTlsVersionAction](forceTlsVersionAction)
 - [setClientCertificateAction](setClientCertificateAction)
 - [skipSslTunnelingAction](skipSslTunnelingAction)

