## execFilter

### Description

Select exchange according to the exit code of a launched process. Evaluation is considered `true` whenthe process exits with 0 error code.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

    execFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| filename | string | The file to be executed |  |
| arguments | string | Command line arguments |  |
| writeHeaderToEnv | boolean | When this value is set to true, the request header will written under env var `Exec.RequestHeader` with HTTP/1.1 syntax | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

A filter running `true` process and allowing any exchanges.

```yaml
rules:
- filter:
    typeKind: ExecFilter
    filename: true
    arguments: ''
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



