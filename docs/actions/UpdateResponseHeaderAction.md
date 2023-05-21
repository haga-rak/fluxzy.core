## updateResponseHeaderAction

### Description

Update and existing response header. If the header does not exists in the original response, the header will be added.<br/>Use {{previous}} keyword to refer to the original value of the header.<br/><strong>Note</strong> Headers that alter the connection behaviour will be ignored.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.

### YAML configuration name

    updateResponseHeaderAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| headerName | string |  |  |
| headerValue | string |  |  |

### Example of usage

The following examples apply this action to any exchanges

Update the Server header.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: UpdateResponseHeaderAction
    headerName: Server
    headerValue: Fluxzy
```



