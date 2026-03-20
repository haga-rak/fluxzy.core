## protobufFilter

### Description

Select exchanges having a protobuf request or response body. Filtering is made by inspecting value of `Content-Type` header on both request and response.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    protobufFilter

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

Select exchanges with protobuf request or response body.

```yaml
rules:
- filter:
    typeKind: ProtobufFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select exchanges without any protobuf body.

```yaml
rules:
- filter:
    typeKind: ProtobufFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [ProtobufFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.ProtobufFilter.html) for .NET integration.

### See also

This filter has no related filter

