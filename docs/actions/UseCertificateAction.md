## useCertificateAction

### Description

Use a specific server certificate. Certificate can be retrieved from user store or from a PKCS12 file

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

useCertificateAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| serverCertificate.retrieveMode | fluxzyDefault \| fromUserStoreSerialNumber \| fromUserStoreThumbPrint \| fromPkcs12 | Retrieve mode |  |
| serverCertificate.serialNumber | string | Serial number of a certificate available on user store |  |
| serverCertificate.thumbPrint | string | Thumbprint of a certificate available on user store (hex format) |  |
| serverCertificate.pkcs12File | string | Path to a PKCS#12 certificate |  |
| serverCertificate.pkcs12Password | string | Certificate passphrase when Pkcs12File is defined |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Use a certificate with serial number `xxxxxx` retrieved from for local user as a server certificate.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: UseCertificateAction
    serverCertificate:
      retrieveMode: FromUserStoreSerialNumber
      serialNumber: xxxxxx
```



### .NET reference

View definition of [UseCertificateAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.UseCertificateAction.html) for .NET integration.

### See also

This action has no related action

