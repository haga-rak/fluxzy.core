## applyTagAction

### Description

Affect a tag to exchange. Tags are meta-information and do not alter the connection.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**onAuthorityReceived** This scope denotes the moment fluxzy is aware the destination authority. In a regular proxy connection, it will occur the moment where fluxzy parsed the CONNECT request.
:::

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
      identifier: 852d1563-5664-4f17-a4f2-bfe5f7c4993a
      value: Hello fluxzy
```



### See also

The following actions are related to this action: 

 - [applyCommentAction](applyCommentAction)
 - [applyTagAction](applyTagAction)

