## skipRemoteCertificateValidationAction

### Description

Skip validating remote certificate. Fluxzy will ignore any validation errors on the server certificate.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

### YAML configuration name

skipRemoteCertificateValidationAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Skip validating remote certificate. Fluxzy will ignore any validation errors on the server certificate.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SkipRemoteCertificateValidationAction
    noEditableSetting: true
```



