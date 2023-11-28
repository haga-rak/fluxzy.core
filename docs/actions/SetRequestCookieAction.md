## setRequestCookieAction

### Description

Add a cookie to request. This action is performed by adding/replacing `Cookie` header in request.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

setRequestCookieAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string | Cookie name |  |
| value | string | Cookie value |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Add request cookie with name `session` and value `123456`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetRequestCookieAction
    name: session
    value: 123456
```



### .NET reference

View definition of [SetRequestCookieAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.SetRequestCookieAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [setRequestCookieAction](setRequestCookieAction)
 - [setResponseCookieAction](setResponseCookieAction)

