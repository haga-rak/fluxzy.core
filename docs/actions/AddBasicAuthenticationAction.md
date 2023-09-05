## addBasicAuthenticationAction

### Description

Add a basic authentication (RFC 7617) to incoming exchanges with an username and a password

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

addBasicAuthenticationAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| username | string |  |  |
| password | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Add a basic authentication with username `lilou` and password `multipass`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: AddBasicAuthenticationAction
    username: lilou
    password: multipass
```



