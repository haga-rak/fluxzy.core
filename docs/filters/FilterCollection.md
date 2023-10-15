## filterCollection

### Description

FilterCollection is a combination of multiple filters with a merging operator (OR / AND).

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

    filterCollection

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| children | array of filters | Children filters | [] |
| operation | or \| and | Condition evaluation | or |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Retains exchanges having POST as method OR request to the host example.com.

```yaml
rules:
- filter:
    typeKind: FilterCollection
    children:
    - typeKind: PostFilter
    - typeKind: HostFilter
      pattern: example.com
      operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### See also

This filter has no related filter

