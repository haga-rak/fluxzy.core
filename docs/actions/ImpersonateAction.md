## impersonateAction

### Description

Impersonate a browser or client by changing the TLS fingerprint, HTTP/2 settings and headers.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

impersonateAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| nameOrConfigFile | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Impersonate CHROME 131 on Windows.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ImpersonateAction
    nameOrConfigFile: Chrome_Windows_131
```



### .NET reference

View definition of [ImpersonateAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ImpersonateAction.html) for .NET integration.

### See also

This action has no related action

