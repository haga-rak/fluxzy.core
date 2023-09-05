## hasCookieOnRequestFilter

### Description

Exchange having a request cookie with a specific name

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    hasCookieOnRequestFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string | Cookie name |  |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | contains |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Select only filter having a request cookie with name `JSESSIONID`.

```yaml
rules:
- filter:
    typeKind: HasCookieOnRequestFilter
    name: JSESSIONID
    pattern: ''
    operation: Regex
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



