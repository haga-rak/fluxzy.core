## applyTagAction

### Description

Affect a tag to exchange. Tags are meta-information and do not alter the connection.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.

### YAML configuration name

    applyTagAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Add tag `Hello fluxzy`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ApplyTagAction
    tag:
      identifier: de4cfaef-4bf0-4a89-89f9-e620ca7096ba
      value: Hello fluxzy
```



