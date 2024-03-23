## upStreamProxyAction

### Description

Use an upstream proxy.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

upStreamProxyAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| host | string |  |  |
| port | int32 |  | 0 |
| proxyAuthorizationHeader | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Use an upstream proxy to 192.168.1.9 on port 8080.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: UpStreamProxyAction
    host: 192.168.1.9
    port: 8080
```


Use an upstream proxy to 192.168.1.9 on port 8080 with basic auth login: leeloo , password: multipass.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: UpStreamProxyAction
    host: 192.168.1.9
    port: 8080
    proxyAuthorizationHeader: Basic bGVlbG9vOm11bHRpcGFzcw==
```



### .NET reference

View definition of [UpStreamProxyAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.UpStreamProxyAction.html) for .NET integration.

### See also

This action has no related action

