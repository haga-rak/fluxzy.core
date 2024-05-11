## addRequestHeaderAction

### Description

Append a request header.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

addRequestHeaderAction

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

Add DNT = 1 header to any requests.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: AddRequestHeaderAction
    headerName: DNT
    headerValue: 1
```


Add a request cookie with name `cookie_name` and value `cookie_value`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: AddRequestHeaderAction
    headerName: Cookie
    headerValue: cookie_name=cookie_value
```



### .NET reference

View definition of [AddRequestHeaderAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.AddRequestHeaderAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [addRequestHeaderAction](addRequestHeaderAction)
 - [addResponseHeaderAction](addResponseHeaderAction)
 - [updateRequestHeaderAction](updateRequestHeaderAction)
 - [updateResponseHeaderAction](updateResponseHeaderAction)
 - [deleteRequestHeaderAction](deleteRequestHeaderAction)
 - [deleteResponseHeaderAction](deleteResponseHeaderAction)
 - [setUserAgentAction](setUserAgentAction)

