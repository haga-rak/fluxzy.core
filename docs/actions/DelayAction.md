## delayAction

### Description

Add a latency to the exchange.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

delayAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| duration | int32 | Duration in milliseconds | 0 |
| scope | onAuthorityReceived \| requestHeaderReceivedFromClient \| dnsSolveDone \| requestBodyReceivedFromClient \| responseHeaderReceivedFromRemote \| responseBodyReceivedFromRemote \| copySibling \| outOfScope | Define when the delay is applied | responseHeaderReceivedFromRemote |

:::
### Example of usage

This filter has no specific usage example


### .NET reference

View definition of [DelayAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.DelayAction.html) for .NET integration.

### See also

This action has no related action

