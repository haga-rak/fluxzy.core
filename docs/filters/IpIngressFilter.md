## ipIngressFilter

### Description

Select exchanges according to client ip address. Full IP notation is used from IPv6.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

    ipIngressFilter

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

Retains only exchanges coming from IP 192.168.1.1.

```yaml
rules:
- filter:
    typeKind: IpIngressFilter
    pattern: 192.168.1.1
    operation: EndsWith
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [IpIngressFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.IpIngressFilter.html) for .NET integration.

### See also

This filter has no related filter

