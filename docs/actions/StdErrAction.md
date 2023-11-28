## stdErrAction

### Description

Write text to standard error. Captured variable are interpreted.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**copySibling** Applied only on action. The action associated with this scope will copy his value from the triggering filter.
:::

### YAML configuration name

stdErrAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| text | string |  |  |
| runScope | nullable`1 | When RunScope is defined. The action is only evaluated when the value of the scope occured. |  |

:::
### Example of usage

This filter has no specific usage example


### .NET reference

View definition of [StdErrAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.StdErrAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [fileAppendAction](fileAppendAction)
 - [stdOutAction](stdOutAction)
 - [stdErrAction](stdErrAction)

