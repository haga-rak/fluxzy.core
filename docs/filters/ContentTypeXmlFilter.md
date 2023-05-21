## contentTypeXmlFilter

### Description

Select exchanges having XML response body.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.

### YAML configuration name

    contentTypeXmlFilter

### Settings

This filter has no specific characteristic

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges having XML response body.

```yaml
rules:
- filter:
    typeKind: ContentTypeXmlFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



