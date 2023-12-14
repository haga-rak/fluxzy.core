## injectIntoHtmlTagAction

### Description

This action analyze a  response body and inject a text after the first a specified html tag. This action relies on ExchangeContext.ResponseBodySubstitution to perform the injection. This action is issued essentially to inject a script tag in a html page.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

injectIntoHtmlTagAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| tag | string | Html tag name after which the injection will be performed |  |
| text | string | The text to be injected |  |
| fromFile | boolean | If true, the text will be read from a file | false |
| fileName | string | If FromFile is true, the file name to read from |  |
| encoding | string | IANA name encoding |  |
| restrictToHtml | boolean | Restrict substitution to text/html response | true |

:::
### Example of usage

This filter has no specific usage example


### .NET reference

View definition of [InjectIntoHtmlTagAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.InjectIntoHtmlTagAction.html) for .NET integration.

### See also

This action has no related action

