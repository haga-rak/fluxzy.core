## hasCommentFilter

### Description

Select exchanges having comment.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**outOfScope** Means that the filter or action associated to this scope won't be trigger in the regular HTTP flow. This scope is applied only on view filter and internal actions.
:::

### YAML configuration name

    hasCommentFilter

### Settings

This filter has no specific characteristic

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges having comment.

```yaml
rules:
- filter:
    typeKind: HasCommentFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



