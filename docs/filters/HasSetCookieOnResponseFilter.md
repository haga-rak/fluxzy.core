## hasSetCookieOnResponseFilter

### Description

Search for a cookie value present in a `set-cookie` header response.If cookie name is not defined or empty, the filter will returns any cookie having the value.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    hasSetCookieOnResponseFilter

### Settings

The following table describes the customizable properties available for this filter: 

{.property-table .property-table-filter}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string | Cookie name. Leave empty to match any cookies. This field is case sensitive |  |
| pattern | string | The string pattern to search |  |
| operation | exact \| contains \| startsWith \| endsWith \| regex | The search operation performed | contains |
| caseSensitive | boolean | true if the Search should be case sensitive | false |
| inverted | boolean | Negate the filter result | false |
:::

### Example of usage

This filter has no specific usage example

### .NET reference

View definition of [HasSetCookieOnResponseFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.HasSetCookieOnResponseFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [hasAnyCookieOnRequestFilter](hasAnyCookieOnRequestFilter)
 - [hasCookieOnRequestFilter](hasCookieOnRequestFilter)
 - [hasSetCookieOnResponseFilter](hasSetCookieOnResponseFilter)

