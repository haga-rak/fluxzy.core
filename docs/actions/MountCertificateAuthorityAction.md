## mountCertificateAuthorityAction

### Description

Reply with the default root certificate used by fluxzy

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    mountCertificateAuthorityAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Reply with the default root certificate used by fluxzy.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: MountCertificateAuthorityAction
    noEditableSetting: true
```



