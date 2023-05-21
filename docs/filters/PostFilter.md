## postFilter

### Description

Select POST (request method) only exchanges.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    postFilter

### Settings

This filter has no specific characteristic

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |

### Example of usage

The following examples apply a comment to the filtered exchange

Select POST (request method) only exchanges.

```yaml
rules:
- filter:
    typeKind: PostFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



