## formRequestFilter

### Description

Select request sending 'multipart/form-data' or 'application/x-www-form-urlencoded' body. Filtering is made by inspecting value of `Content-Type` header

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    formRequestFilter

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

Retains exchanges having POST as method OR request to the host example.com.

```yaml
rules:
- filter:
    typeKind: FilterCollection
    children:
    - typeKind: PostFilter
    - typeKind: HostFilter
      pattern: example.com
      operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [FormRequestFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.FormRequestFilter.html) for .NET integration.

### See also

This filter has no related filter

