## getFilter

### Description

Select exchanges with GET method

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    getFilter

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

Select exchanges with GET method.

```yaml
rules:
- filter:
    typeKind: GetFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [GetFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.GetFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [methodFilter](methodFilter)
 - [getFilter](getFilter)
 - [postFilter](postFilter)
 - [putFilter](putFilter)
 - [deleteFilter](deleteFilter)
 - [patchFilter](patchFilter)

