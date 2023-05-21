## skipSslTunnelingAction

### Description

Instructs fluxzy to not decrypt the current traffic. The associated filter  must be on OnAuthorityReceived scope in order to make this action effective. 

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.

### YAML configuration name

    skipSslTunnelingAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Instructs fluxzy to not decrypt the current traffic. The associated filter  must be on OnAuthorityReceived scope in order to make this action effective.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SkipSslTunnelingAction
```



