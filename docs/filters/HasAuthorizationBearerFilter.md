## hasAuthorizationBearerFilter

### Description

Select exchanges having bearer token in authorization.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    hasAuthorizationBearerFilter

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

Select exchanges having bearer token in authorization.

```yaml
rules:
- filter:
    typeKind: HasAuthorizationBearerFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



