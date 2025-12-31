## addAuthorizationBearerAction

### Description

Add Authorization Bearer token to the request header.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

addAuthorizationBearerAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| token | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Add Authorization Bearer token to the request header.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: AddAuthorizationBearerAction
    token: your_token_here
```



### .NET reference

View definition of [AddAuthorizationBearerAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.AddAuthorizationBearerAction.html) for .NET integration.

### See also

This action has no related action

