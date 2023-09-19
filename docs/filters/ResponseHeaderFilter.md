## responseHeaderFilter

### Description

Select exchanges according to response header values.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    responseHeaderFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| headerName | string | Header name |  |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | contains |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Retains only exchanges with a strict-transport-security response header.

```yaml
rules:
- filter:
    typeKind: ResponseHeaderFilter
    headerName: strict-transport-security
    pattern: .*
    operation: Regex
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



