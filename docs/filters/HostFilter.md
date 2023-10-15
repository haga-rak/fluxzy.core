## hostFilter

### Description

Select exchanges according to hostname (excluding port). To select authority (combination of host:port), use <goto>AuthorityFilter</goto>.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

    hostFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | exact |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Retains only exchanges with the exact host.

```yaml
rules:
- filter:
    typeKind: HostFilter
    pattern: www.fluxzy.io
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Retains only exchanges with a host matching the regex.

```yaml
rules:
- filter:
    typeKind: HostFilter
    pattern: ^www\.fluxzy\.io$
    operation: Regex
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

