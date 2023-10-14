## agentLabelFilter

### Description

Select exchanges according to configured source agent (user agent or process) with a regular string search.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    agentLabelFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | contains |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Retains only exchanges with the exact agent label.

```yaml
rules:
- filter:
    typeKind: AgentLabelFilter
    pattern: Chrome
    operation: Contains
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### See also

This filter has no related filter

