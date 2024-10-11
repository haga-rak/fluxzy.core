## idLookupFilter

### Description

Select exchange with ids. `-` is used to define a range. `,` is used to separate values

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

    idLookupFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges with id 5.

```yaml
rules:
- filter:
    typeKind: IdLookupFilter
    pattern: 5
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select exchanges with from 2 to 5.

```yaml
rules:
- filter:
    typeKind: IdLookupFilter
    pattern: 2-5
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select exchanges with ids 3,4,9.

```yaml
rules:
- filter:
    typeKind: IdLookupFilter
    pattern: 3,4,9
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [IdLookupFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.IdLookupFilter.html) for .NET integration.

### See also

This filter has no related filter

