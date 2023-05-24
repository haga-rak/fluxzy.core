## deleteRequestHeaderAction

### Description

Remove request headers. This action removes <b>every</b> occurrence of the header from the request.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    deleteRequestHeaderAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| headerName | string |  |  |

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


