## addResponseHeaderAction

### Description

Append a response header. H2 pseudo header will be ignored.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

addResponseHeaderAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| headerName | string |  |  |
| headerValue | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Add a `content-security-policy` header on response.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: AddResponseHeaderAction
    headerName: content-security-policy
    headerValue: default-src 'none'
```



### See also

The following actions are related to this action: 

 - [addRequestHeaderAction](addRequestHeaderAction)
 - [addResponseHeaderAction](addResponseHeaderAction)
 - [updateRequestHeaderAction](updateRequestHeaderAction)
 - [updateResponseHeaderAction](updateResponseHeaderAction)
 - [deleteRequestHeaderAction](deleteRequestHeaderAction)
 - [deleteResponseHeaderAction](deleteResponseHeaderAction)

