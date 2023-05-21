## authorityFilter

### Description

Select exchange according to hostname and a port

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.

### YAML configuration name

    authorityFilter

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | 0 |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |

### Example of usage

The following examples apply a comment to the filtered exchange

Select only request from host `fluxzy.io` at port 8080.

```yaml
rules:
- filter:
    typeKind: AuthorityFilter
    port: 8080
    pattern: fluxzy.io
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select any exchanges going to a subdomain of `google.com` at port 443.

```yaml
rules:
- filter:
    typeKind: AuthorityFilter
    port: 443
    pattern: google.com
    operation: EndsWith
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



