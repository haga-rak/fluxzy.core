## noResponseFilter

### Description

Select exchanges that received no response from the remote (request sent, connection closed or proxy stopped before any response header arrived). Exchanges failed by fluxzy itself (528, DNS or connection errors) are excluded.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**outOfScope** Means that the filter or action associated to this scope won't be trigger in the regular HTTP flow. This scope is applied only on view filter and internal actions.
:::

### YAML configuration name

    noResponseFilter

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

Select exchanges that received no response from the remote (request sent, connection closed or proxy stopped before any response header arrived). Exchanges failed by fluxzy itself (528, DNS or connection errors) are excluded.

```yaml
rules:
- filter:
    typeKind: NoResponseFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [NoResponseFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.ResponseFilters.NoResponseFilter.html) for .NET integration.

### See also

This filter has no related filter

