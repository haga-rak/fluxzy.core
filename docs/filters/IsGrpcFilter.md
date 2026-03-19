## isGrpcFilter

### Description

Select gRPC exchanges only. Filtering is made by inspecting value of `Content-Type` header.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    isGrpcFilter

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

Select only gRPC exchanges.

```yaml
rules:
- filter:
    typeKind: IsGrpcFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select all non-gRPC exchanges.

```yaml
rules:
- filter:
    typeKind: IsGrpcFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [IsGrpcFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.IsGrpcFilter.html) for .NET integration.

### See also

This filter has no related filter

