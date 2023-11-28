## isSecureFilter

### Description

Select secure exchange only (non plain HTTP).

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

    isSecureFilter

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

Select secure exchange only (non plain HTTP).

```yaml
rules:
- filter:
    typeKind: IsSecureFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [IsSecureFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.IsSecureFilter.html) for .NET integration.

### See also

This filter has no related filter

