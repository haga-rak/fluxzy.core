## injectHtmlTagAction

### Description

This action analyze a  response body and inject a text after the first provided html tag.This action relies on ExchangeContext.ResponseBodySubstitution to perform the injection. This action is issued essentially to inject a script tag in a html page.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

injectHtmlTagAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Inject a CSS style tag after `<head>` that sets the document body color to red.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: InjectHtmlTagAction
    tag: head
    htmlContent: '<style>body { background-color: red !important; }</style>'
    restrictToHtml: true
```


Inject a  file after `<head>`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: InjectHtmlTagAction
    tag: head
    fromFile: true
    fileName: injected.html
    restrictToHtml: true
```



### .NET reference

View definition of [InjectHtmlTagAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.InjectHtmlTagAction.html) for .NET integration.

### See also

This action has no related action

