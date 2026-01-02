## processNameFilter

### Description

Select exchanges initiated by processes with the specified names. Process names are matched case-insensitively. On Windows, the .exe extension can be omitted. Process tracking must be enabled and the connection must originate from localhost.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    processNameFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| processNames | list`1 | List of process names to match | system.Collections.Generic.List`1[System.String] |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Filter traffic from curl process.

```yaml
rules:
- filter:
    typeKind: ProcessNameFilter
    processNames:
    - curl
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Filter traffic from curl or wget processes.

```yaml
rules:
- filter:
    typeKind: ProcessNameFilter
    processNames:
    - curl
    - wget
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [ProcessNameFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.ProcessNameFilter.html) for .NET integration.

### See also

This filter has no related filter

