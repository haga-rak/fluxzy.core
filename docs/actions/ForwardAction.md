## forwardAction

### Description

Forward request to a specific URL. This action makes fluxzy act as a reverse proxy. Unlike [SpoofDnsAction](https://www.fluxzy.io/rule/item/spoofDnsAction), host header is automatically set and protocol switch is supported (http to https, http/1.1 to h2, ...). The URL must be an absolute path.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

forwardAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| url | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Forward any request to https://www.example.com.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ForwardAction
    url: https://www.example.com
```



### .NET reference

View definition of [ForwardAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ForwardAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [forwardAction](forwardAction)
 - [spoofDnsAction](spoofDnsAction)
 - [serveDirectoryAction](serveDirectoryAction)
 - [mockedResponseAction](mockedResponseAction)
 - [injectHtmlTagAction](injectHtmlTagAction)

