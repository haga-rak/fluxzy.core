## applyCommentAction

### Description

Add comment to exchange. Comment has no effect on the stream behaviour.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.

### YAML configuration name

    applyCommentAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| comment | string |  |  |

### Example of usage

The following examples apply this action to any exchanges

Add comment `Hello fluxzy`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: Hello fluxzy
```



