## statusCodeRedirectionFilter

### Description

Select exchanges that HTTP status code indicates a redirect (3XX).

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    statusCodeRedirectionFilter

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

Select exchanges that HTTP status code indicates a redirect (3XX).

```yaml
rules:
- filter:
    typeKind: StatusCodeRedirectionFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [StatusCodeRedirectionFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.ResponseFilters.StatusCodeRedirectionFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [statusCodeFilter](statusCodeFilter)
 - [statusCodeSuccessFilter](statusCodeSuccessFilter)
 - [statusCodeRedirectionFilter](statusCodeRedirectionFilter)
 - [statusCodeClientErrorFilter](statusCodeClientErrorFilter)
 - [statusCodeServerErrorFilter](statusCodeServerErrorFilter)

