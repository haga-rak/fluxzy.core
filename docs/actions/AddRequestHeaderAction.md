## addRequestHeaderAction

### Description

Append a request header.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    addRequestHeaderAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| headerName | string |  |  |
| headerValue | string |  |  |

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


