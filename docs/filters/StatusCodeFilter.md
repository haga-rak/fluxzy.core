## statusCodeFilter

### Description

Select exchanges according to HTTP status code.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    statusCodeFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| statusCodes | list`1 | List of status code | system.Collections.Generic.List`1[System.Int32] |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

This filter has no specific usage example

### See also

The following filters are related to this filter: 

 - [statusCodeFilter](statusCodeFilter)
 - [statusCodeSuccessFilter](statusCodeSuccessFilter)
 - [statusCodeRedirectionFilter](statusCodeRedirectionFilter)
 - [statusCodeClientErrorFilter](statusCodeClientErrorFilter)
 - [statusCodeServerErrorFilter](statusCodeServerErrorFilter)

