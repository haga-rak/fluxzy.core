## formUrlEncodedRequestFilter

### Description

Select request sending 'application/x-www-form-urlencoded' body. Filtering is made by inspecting value of `Content-Type` header

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    formUrlEncodedRequestFilter

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

Select exchanges having request header `dnt: 1`.

```yaml
rules:
- filter:
    typeKind: RequestHeaderFilter
    headerName: dnt
    pattern: 1
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select exchanges issued by Chrome 112 by checking User-Agent.

```yaml
rules:
- filter:
    typeKind: RequestHeaderFilter
    headerName: User-Agent
    pattern: 'Chrome/112 '
    operation: Contains
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [FormUrlEncodedRequestFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.FormUrlEncodedRequestFilter.html) for .NET integration.

### See also

This filter has no related filter

