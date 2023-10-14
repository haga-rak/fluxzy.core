## changeRequestMethodAction

### Description

Alter the method of an exchange.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

changeRequestMethodAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| newMethod | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Change request method PATCH.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ChangeRequestMethodAction
    newMethod: PATCH
```



### See also

The following actions are related to this action: 

 - [changeRequestMethodAction](changeRequestMethodAction)
 - [changeRequestPathAction](changeRequestPathAction)

