## protobufRequestFilter

### Description

Select requests sending a protobuf body. Filtering is made by inspecting value of `Content-Type` header.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestBodyReceivedFromClient** This scope occurs the moment fluxzy received fully the request body from the client. In a fullstreaming mode which is the default mode, this event occurs when the full body is already fully sent to the remote server. 
:::

### YAML configuration name

    protobufRequestFilter

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

Select only exchanges with protobuf request body.

```yaml
rules:
- filter:
    typeKind: ProtobufRequestFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select exchanges without protobuf request body.

```yaml
rules:
- filter:
    typeKind: ProtobufRequestFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [ProtobufRequestFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.ProtobufRequestFilter.html) for .NET integration.

### See also

This filter has no related filter

