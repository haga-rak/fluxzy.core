## absoluteUriFilter

### Description

Select exchanges according to URI (scheme, FQDN, path and query). Supports common string search option and regular expression.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    absoluteUriFilter

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

Select only exchanges with the URL matching exactly `https://www.fluxzy.io/some-path`.

```yaml
rules:
- filter:
    typeKind: AbsoluteUriFilter
    pattern: https://www.fluxzy.io/some-path
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Match all HTTPS exchanges by checking URL scheme with a regular expression.

```yaml
rules:
- filter:
    typeKind: AbsoluteUriFilter
    pattern: ^https\:\/\/
    operation: Regex
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



