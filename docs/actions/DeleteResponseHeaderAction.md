## deleteResponseHeaderAction

### Description

Remove response headers. This action removes <b>every</b> occurrence of the header from the response.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

deleteResponseHeaderAction

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

Remove every Set-Cookie header from response.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: DeleteResponseHeaderAction
    headerName: Set-Cookie
```



### .NET reference

View definition of [DeleteResponseHeaderAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.DeleteResponseHeaderAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [addRequestHeaderAction](addRequestHeaderAction)
 - [addResponseHeaderAction](addResponseHeaderAction)
 - [updateRequestHeaderAction](updateRequestHeaderAction)
 - [updateResponseHeaderAction](updateResponseHeaderAction)
 - [deleteRequestHeaderAction](deleteRequestHeaderAction)
 - [deleteResponseHeaderAction](deleteResponseHeaderAction)

