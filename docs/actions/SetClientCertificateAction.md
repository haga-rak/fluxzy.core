## setClientCertificateAction

### Description

Add a client certificate to the exchange. The client certificate will be used for establishing the mTLS authentication if the remote request it. The client certificate can be retrieved from the default store (my) or from a PKCS#12 file (.p12, pfx). <br/>The certificate will not be stored in fluxzy settings and, therefore, must be available at runtime. 

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.

### YAML configuration name

    setClientCertificateAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| clientCertificate | certificate |  | fluxzy.Certificates.Certificate |

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


