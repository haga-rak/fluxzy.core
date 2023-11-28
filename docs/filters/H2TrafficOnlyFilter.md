## h2TrafficOnlyFilter

### Description

Select H2 exchanges only.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    h2TrafficOnlyFilter

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

Select H2 exchanges only.

```yaml
rules:
- filter:
    typeKind: H2TrafficOnlyFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [H2TrafficOnlyFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.H2TrafficOnlyFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [h11TrafficOnlyFilter](h11TrafficOnlyFilter)
 - [h2TrafficOnlyFilter](h2TrafficOnlyFilter)

