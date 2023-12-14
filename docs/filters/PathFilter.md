## pathFilter

### Description

Select exchanges according to url path. Path includes query string if any. Path must start with `/`

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    pathFilter

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

Retains only exchanges having uri starting with API.

```yaml
rules:
- filter:
    typeKind: PathFilter
    pattern: /api
    operation: StartsWith
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [PathFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.PathFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [absoluteUriFilter](absoluteUriFilter)
 - [hostFilter](hostFilter)
 - [authorityFilter](authorityFilter)
 - [pathFilter](pathFilter)
 - [queryStringFilter](queryStringFilter)
 - [requestHeaderFilter](requestHeaderFilter)

