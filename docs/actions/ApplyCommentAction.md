## applyCommentAction

### Description

Add comment to exchange. Comment has no effect on the stream behaviour.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

applyCommentAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| comment | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Add comment `Hello fluxzy`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: Hello fluxzy
```



### .NET reference

View definition of [ApplyCommentAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ApplyCommentAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [applyCommentAction](applyCommentAction)
 - [applyTagAction](applyTagAction)

