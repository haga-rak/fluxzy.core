## hasAnyCookieOnRequestFilter

### Description

Select exchanges having any request cookie

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

    hasAnyCookieOnRequestFilter

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

Select exchanges having any request cookie.

```yaml
rules:
- filter:
    typeKind: HasAnyCookieOnRequestFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [HasAnyCookieOnRequestFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.RequestFilters.HasAnyCookieOnRequestFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [hasAnyCookieOnRequestFilter](hasAnyCookieOnRequestFilter)
 - [hasCookieOnRequestFilter](hasCookieOnRequestFilter)
 - [hasSetCookieOnResponseFilter](hasSetCookieOnResponseFilter)

