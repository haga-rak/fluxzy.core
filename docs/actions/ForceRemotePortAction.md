## forceRemotePortAction

### Description

Ignores the default port used by the current authority and use the provided port instead.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

forceRemotePortAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| port | int32 | The port to use for the remote connection | 0 |

:::
### Example of usage

This filter has no specific usage example


### .NET reference

View definition of [ForceRemotePortAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ForceRemotePortAction.html) for .NET integration.

### See also

This action has no related action

