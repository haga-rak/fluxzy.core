## fileAppendAction

### Description

Write to a file. Captured variable are interpreted.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**copySibling** Applied only on action. The action associated with this scope will copy his value from the triggering filter.
:::

### YAML configuration name

fileAppendAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| filename | string | Filename |  |
| text | string | Text to write |  |
| encoding | string | Default encoding. UTF-8 if not any. |  |
| runScope | nullable`1 | When RunScope is defined. The action is only evaluated when the value of the scope occured. |  |

:::
### Example of usage

This filter has no specific usage example


### .NET reference

View definition of [FileAppendAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.FileAppendAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [fileAppendAction](fileAppendAction)
 - [stdOutAction](stdOutAction)
 - [stdErrAction](stdErrAction)

