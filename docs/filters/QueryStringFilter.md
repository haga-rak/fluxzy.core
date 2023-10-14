## queryStringFilter

### Description

Select exchanges containing a specific query string. If `name` is not defined or empty, the search will be performed on any query string values.The search will pass if at least one value match.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    queryStringFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string | The query string name |  |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | exact |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges having query string `id=123456`.

```yaml
rules:
- filter:
    typeKind: QueryStringFilter
    name: id
    pattern: 123456
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### See also

The following filters are related to this filter: 

 - [absoluteUriFilter](absoluteUriFilter)
 - [hostFilter](hostFilter)
 - [authorityFilter](authorityFilter)
 - [pathFilter](pathFilter)
 - [queryStringFilter](queryStringFilter)
 - [requestHeaderFilter](requestHeaderFilter)

