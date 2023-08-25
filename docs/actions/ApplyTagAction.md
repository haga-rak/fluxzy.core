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
      identifier: 70515ed8-e270-4854-8a0c-0560daca7a75
      value: Hello fluxzy
```



