## statusCodeSuccessFilter

### Description

Select exchanges that HTTP status code indicates a successful request (2XX).

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    statusCodeSuccessFilter

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

Select exchanges that HTTP status code indicates a successful request (2XX).

```yaml
rules:
- filter:
    typeKind: StatusCodeSuccessFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### See also

The following filters are related to this filter: 

 - [statusCodeFilter](statusCodeFilter)
 - [statusCodeSuccessFilter](statusCodeSuccessFilter)
 - [statusCodeRedirectionFilter](statusCodeRedirectionFilter)
 - [statusCodeClientErrorFilter](statusCodeClientErrorFilter)
 - [statusCodeServerErrorFilter](statusCodeServerErrorFilter)

