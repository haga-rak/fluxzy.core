## setClientCertificateAction

### Description

Add a client certificate to the exchange. The client certificate will be used for establishing the mTLS authentication if the remote request it. The client certificate can be retrieved from the default store (my) or from a PKCS#12 file (.p12, pfx). <br/>The certificate will not be stored in fluxzy settings and, therefore, must be available at runtime. 

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

setClientCertificateAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| clientCertificate.retrieveMode | fluxzyDefault \| fromUserStoreSerialNumber \| fromUserStoreThumbPrint \| fromPkcs12 | Retrieve mode |  |
| clientCertificate.serialNumber | string | Serial number of a certificate available on user store |  |
| clientCertificate.thumbPrint | string | Thumbprint of a certificate available on user store (hex format) |  |
| clientCertificate.pkcs12File | string | Path to a PKCS#12 certificate |  |
| clientCertificate.pkcs12Password | string | Certificate passphrase when Pkcs12File is defined |  |
| alwaysSendClientCertificate | boolean |  | false |

:::
### Example of usage

The following examples apply this action to any exchanges

Use a certificate with serial number `xxxxxx` retrieved from for local user store to establish mTLS authentication.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetClientCertificateAction
    clientCertificate:
      retrieveMode: FromUserStoreSerialNumber
      serialNumber: xxxxxx
```



### .NET reference

View definition of [SetClientCertificateAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.SetClientCertificateAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [forceHttp11Action](forceHttp11Action)
 - [forceHttp2Action](forceHttp2Action)
 - [forceTlsVersionAction](forceTlsVersionAction)
 - [setClientCertificateAction](setClientCertificateAction)
 - [skipSslTunnelingAction](skipSslTunnelingAction)
 - [useDnsOverHttpsAction](useDnsOverHttpsAction)

