## serveHttp11Action

### Description

Force the downstream connection (client-to-proxy) to use HTTP/1.1. When the global ServeH2 option is enabled, this action overrides it for matched exchanges by only advertising HTTP/1.1 during ALPN negotiation with the client.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

serveHttp11Action

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Force HTTP/1.1 serving for a specific host even when ServeH2 is globally enabled.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ServeHttp11Action
```



### .NET reference

View definition of [ServeHttp11Action](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ServeHttp11Action.html) for .NET integration.

### See also

This action has no related action

