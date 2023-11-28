## ipEgressFilter

### Description

Select exchanges according to upstream IP address. Full IP notation is used from IPv6.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

    ipEgressFilter

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

Retains only exchanges where the destination address is `212.12.14.0/24`.

```yaml
rules:
- filter:
    typeKind: IpEgressFilter
    pattern: 212.12.14
    operation: StartsWith
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Retains only exchanges where the destination address is `2a01:cb00:7e2:5000:10d5:70df:665:c654` (IPv6).

```yaml
rules:
- filter:
    typeKind: IpEgressFilter
    pattern: 2a01:cb00:7e2:5000:10d5:70df:665:c654
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [IpEgressFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.IpEgressFilter.html) for .NET integration.

### See also

This filter has no related filter

