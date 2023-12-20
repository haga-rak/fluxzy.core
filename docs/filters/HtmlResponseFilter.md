## htmlResponseFilter

### Description

Select exchanges having HTML body. The content-type header is checked to determine if the content body is has text/html hint.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

    htmlResponseFilter

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

Select exchanges having HTML body. The content-type header is checked to determine if the content body is has text/html hint.

```yaml
rules:
- filter:
    typeKind: HtmlResponseFilter
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


### .NET reference

View definition of [HtmlResponseFilter](https://docs.fluxzy.io/api/Fluxzy.Rules.Filters.ResponseFilters.HtmlResponseFilter.html) for .NET integration.

### See also

The following filters are related to this filter: 

 - [cssStyleFilter](cssStyleFilter)
 - [contentTypeXmlFilter](contentTypeXmlFilter)
 - [fontFilter](fontFilter)
 - [jsonRequestFilter](jsonRequestFilter)
 - [jsonResponseFilter](jsonResponseFilter)
 - [imageFilter](imageFilter)
 - [htmlResponseFilter](htmlResponseFilter)

