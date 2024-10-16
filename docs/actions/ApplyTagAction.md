## applyTagAction

### Description

Affect a tag to exchange. Tags are meta-information and do not alter the connection.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
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



### .NET reference

View definition of [ApplyTagAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.ApplyTagAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [applyCommentAction](applyCommentAction)
 - [applyTagAction](applyTagAction)

