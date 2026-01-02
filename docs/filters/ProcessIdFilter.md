## processIdFilter

### Description

Select exchanges initiated by a process with the specified process ID. Process tracking must be enabled and the connection must originate from localhost.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    processIdFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| processId | int32 | The process ID to match | 0 |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Filter traffic from process with ID 1234.

```yaml
rules:
- filter:
    typeKind: ProcessIdFilter
    processId: 1234
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [ProcessIdFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.ProcessIdFilter.html) for .NET integration.

### See also

This filter has no related filter

