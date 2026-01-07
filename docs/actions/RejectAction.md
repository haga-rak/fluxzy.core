## rejectAction

### Description

Block the request and return HTTP 403 Forbidden response. Use this action to explicitly deny access to specific resources. This is a simple blocking action with no configuration required.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

rejectAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Block access to a specific domain.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RejectAction
```



### .NET reference

View definition of [RejectAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.RejectAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [abortAction](abortAction)
 - [rejectAction](rejectAction)
 - [rejectWithMessageAction](rejectWithMessageAction)
 - [rejectWithStatusCodeAction](rejectWithStatusCodeAction)
 - [mockedResponseAction](mockedResponseAction)

