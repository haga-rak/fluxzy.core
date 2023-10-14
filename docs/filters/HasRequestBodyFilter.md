## hasRequestBodyFilter

### Description

Select request having body.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseBodyReceivedFromRemote** This scope occurs the moment fluxzy received the the response body from the server. In a full streaming mode (which is the default mode), this event occurs the the full body is already sent to the client.
:::

### YAML configuration name

    hasRequestBodyFilter

### Settings

This filter has no specific characteristic

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

The following examples apply a comment to the filtered exchange

Select request having body.

```yaml
rules:
- filter:
    typeKind: HasRequestBodyFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### See also

This filter has no related filter

