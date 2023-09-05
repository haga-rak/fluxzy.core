## requestHeaderFilter

### Description

Select exchanges according to request header values.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    requestHeaderFilter

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

Select exchanges having request header `dnt: 1`.

```yaml
rules:
- filter:
    typeKind: RequestHeaderFilter
    headerName: dnt
    pattern: 1
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select exchanges issued by Chrome 112 by checking User-Agent.

```yaml
rules:
- filter:
    typeKind: RequestHeaderFilter
    headerName: User-Agent
    pattern: 'Chrome/112 '
    operation: Contains
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



