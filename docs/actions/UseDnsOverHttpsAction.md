## useDnsOverHttpsAction

### Description

Use DoH (DNS over HTTPS) to resolve domain names instead of the default DNS provided by the OS

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

useDnsOverHttpsAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| nameOrUrl | string |  |  |
| noCapture | boolean |  | false |

:::
### Example of usage

The following examples apply this action to any exchanges

Use Cloudflare built-in DoH server.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: UseDnsOverHttpsAction
    nameOrUrl: CLOUDFLARE
```


Use provided DoH server: "https://dns.google/resolve". Avoid capturing the DNS requests.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: UseDnsOverHttpsAction
    nameOrUrl: https://dns.google/resolve
    noCapture: true
```



### .NET reference

View definition of [UseDnsOverHttpsAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.UseDnsOverHttpsAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [forceHttp11Action](forceHttp11Action)
 - [forceHttp2Action](forceHttp2Action)
 - [forceTlsVersionAction](forceTlsVersionAction)
 - [setClientCertificateAction](setClientCertificateAction)
 - [skipSslTunnelingAction](skipSslTunnelingAction)
 - [useDnsOverHttpsAction](useDnsOverHttpsAction)

