## putFilter

### Description

Select exchanges according to request method.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    putFilter

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

Select exchanges according to request method.

```yaml
rules:
- filter:
    typeKind: PutFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [PutFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.PutFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [methodFilter](methodFilter)
 - [getFilter](getFilter)
 - [postFilter](postFilter)
 - [putFilter](putFilter)
 - [deleteFilter](deleteFilter)
 - [patchFilter](patchFilter)

