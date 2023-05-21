## forwardAction

### Description

Forward request to a specific URL. This action makes fluxzy act as a reverse proxy. Host header is automatically set. The URL must be an absolute path.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    forwardAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| url | string |  |  |

### Example of usage

The following examples apply this action to any exchanges

Forward any request to https://www.example.com.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ForwardAction
    url: htts://www.example.com
```



