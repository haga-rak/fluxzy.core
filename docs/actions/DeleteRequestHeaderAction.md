## deleteRequestHeaderAction

### Description

Remove request headers. This action removes <b>every</b> occurrence of the header from the request.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

deleteRequestHeaderAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| headerName | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Remove every Cookie header from request.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: DeleteRequestHeaderAction
    headerName: Cookie
```



### .NET reference

View definition of [DeleteRequestHeaderAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.DeleteRequestHeaderAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [addRequestHeaderAction](addRequestHeaderAction)
 - [addResponseHeaderAction](addResponseHeaderAction)
 - [updateRequestHeaderAction](updateRequestHeaderAction)
 - [updateResponseHeaderAction](updateResponseHeaderAction)
 - [deleteRequestHeaderAction](deleteRequestHeaderAction)
 - [deleteResponseHeaderAction](deleteResponseHeaderAction)

