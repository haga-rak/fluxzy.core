## pinUpstreamConnectionAction

### Description

Pin the upstream connection to the originating client connection. Required for connection-oriented authentication (NTLM, Negotiate/Kerberos) to work through the proxy. Pinned exchanges always use HTTP/1.1 upstream.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

pinUpstreamConnectionAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Pin the upstream connection to the originating client connection. Required for connection-oriented authentication (NTLM, Negotiate/Kerberos) to work through the proxy. Pinned exchanges always use HTTP/1.1 upstream.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: PinUpstreamConnectionAction
```



### .NET reference

View definition of [PinUpstreamConnectionAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.PinUpstreamConnectionAction.html) for .NET integration.

### See also

This action has no related action

