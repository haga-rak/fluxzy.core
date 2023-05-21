## contentTypeJsonFilter

### Description

Select exchanges having JSON response body. The content-type header is checked to determine if the content body is a JSON.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.

### YAML configuration name

    contentTypeJsonFilter

### Settings

This filter has no specific characteristic

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges having JSON response body. The content-type header is checked to determine if the content body is a JSON.

```yaml
rules:
- filter:
    typeKind: ContentTypeJsonFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



