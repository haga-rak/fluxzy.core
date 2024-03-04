## removeResponseCookieAction

### Description

Remove a response cookie by setting the expiration date to a past date.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

removeResponseCookieAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string | Cookie name |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Remove a cookie named `JSESSIONID`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RemoveResponseCookieAction
    name: JSESSIONID
```



### .NET reference

View definition of [RemoveResponseCookieAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.RemoveResponseCookieAction.html) for .NET integration.

### See also

This action has no related action

