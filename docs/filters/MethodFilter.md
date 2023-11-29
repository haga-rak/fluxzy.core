## methodFilter

### Description

Select exchanges according to request method.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    methodFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | exact |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges having TRACE request method.

```yaml
rules:
- filter:
    typeKind: MethodFilter
    pattern: TRACE
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [MethodFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.MethodFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [methodFilter](methodFilter)
 - [getFilter](getFilter)
 - [postFilter](postFilter)
 - [putFilter](putFilter)
 - [deleteFilter](deleteFilter)
 - [patchFilter](patchFilter)

