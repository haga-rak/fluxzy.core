## serveDirectoryAction

### Description

Serve a folder as a static web site. This action is made for mocking purpose and not production ready for a web site.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

serveDirectoryAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| directory | string | Directory to serve |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Serve a directory.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ServeDirectoryAction
    directory: /path/to/my/static/website
```



### .NET reference

View definition of [ServeDirectoryAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.ServeDirectoryAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [forwardAction](forwardAction)
 - [spoofDnsAction](spoofDnsAction)
 - [serveDirectoryAction](serveDirectoryAction)
 - [mockedResponseAction](mockedResponseAction)
 - [injectHtmlTagAction](injectHtmlTagAction)

