## fontFilter

### Description

Select exchanges having response content type matching a font payload.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.

### YAML configuration name

    fontFilter

### Settings

This filter has no specific characteristic

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges having response content type matching a font payload.

```yaml
rules:
- filter:
    typeKind: FontFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



