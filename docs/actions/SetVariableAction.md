## setVariableAction

### Description

Set a variable or update an existing

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

setVariableAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string | Variable name |  |
| value | string | Variable value |  |
| scope | onAuthorityReceived \| requestHeaderReceivedFromClient \| dnsSolveDone \| requestBodyReceivedFromClient \| responseHeaderReceivedFromRemote \| responseBodyReceivedFromRemote \| copySibling \| outOfScope | The scope where the variable is evaluated | requestHeaderReceivedFromClient |

:::
### Example of usage

This filter has no specific usage example


