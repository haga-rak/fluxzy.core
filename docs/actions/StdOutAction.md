## stdOutAction

### Description

Write text to standard output. Captured variable are interpreted.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**outOfScope** Means that the filter or action associated to this scope won't be trigger in the regular HTTP flow. This scope is applied only on view filter and internal actions.
:::

### YAML configuration name

stdOutAction

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

View definition of [StdOutAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.StdOutAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [fileAppendAction](fileAppendAction)
 - [stdOutAction](stdOutAction)
 - [stdErrAction](stdErrAction)

