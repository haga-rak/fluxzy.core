## networkErrorFilter

### Description

Select exchanges that fails due to network error.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    networkErrorFilter

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

Select exchanges that fails due to network error.

```yaml
rules:
- filter:
    typeKind: NetworkErrorFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [NetworkErrorFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.ResponseFilters.NetworkErrorFilter.html) for .NET integration.

### See also

This filter has no related filter

