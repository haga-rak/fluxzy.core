## changeRequestPathAction

### Description

Change request uri path. This action alters only the path of the request. Request path includes query string.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    changeRequestPathAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| newPath | string |  |  |

### Example of usage

The following examples apply this action to any exchanges

Change request path to `/hello`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ChangeRequestPathAction
    newPath: /hello
```



