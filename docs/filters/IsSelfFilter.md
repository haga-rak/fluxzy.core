## isSelfFilter

### Description

Check if incoming request considers fluxzy as a web server

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**dnsSolveDone** This scope occurs the moment fluxzy ends solving the DNS

### YAML configuration name

    isSelfFilter

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |

### Example of usage

The following examples apply a comment to the filtered exchange

Check if incoming request considers fluxzy as a web server.

```yaml
rules:
- filter:
    typeKind: IsSelfFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



