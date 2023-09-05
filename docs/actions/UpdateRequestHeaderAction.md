## updateRequestHeaderAction

### Description

Update and existing request header. If the header does not exists in the original request, the header will be added. <br/>Use {{previous}} keyword to refer to the original value of the header. <br/><strong>Note</strong> Headers that alter the connection behaviour will be ignored.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

updateRequestHeaderAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| headerName | string |  |  |
| headerValue | string |  |  |
| addIfMissing | boolean |  | false |
| appendSeparator | string | Only active when `AddIfMissing=true` When updating an existing header, this value will be used to separate the original value and the new value. |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Update the User-Agent header.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: UpdateRequestHeaderAction
    headerName: User-Agent
    headerValue: Fluxzy
```



