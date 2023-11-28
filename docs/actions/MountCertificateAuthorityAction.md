## mountCertificateAuthorityAction

### Description

Reply with the default root certificate used by fluxzy

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**dnsSolveDone** This scope occurs the moment fluxzy ends solving the DNS of the remote host
:::

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



### .NET reference

View definition of [MountCertificateAuthorityAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.MountCertificateAuthorityAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [mountCertificateAuthorityAction](mountCertificateAuthorityAction)
 - [mountWelcomePageAction](mountWelcomePageAction)
 - [forwardAction](forwardAction)
 - [spoofDnsAction](spoofDnsAction)
 - [serveDirectoryAction](serveDirectoryAction)

