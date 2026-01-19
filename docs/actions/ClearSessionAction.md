## clearSessionAction

### Description

Clear stored session data for a specific domain or all domains.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

clearSessionAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Clear all stored sessions.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ClearSessionAction
```


Clear session for a specific domain.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ClearSessionAction
    domain: example.com
```



### .NET reference

View definition of [ClearSessionAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.ClearSessionAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [applySessionAction](applySessionAction)
 - [captureSessionAction](captureSessionAction)
 - [clearSessionAction](clearSessionAction)

