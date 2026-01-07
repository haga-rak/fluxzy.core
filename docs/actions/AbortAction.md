## abortAction

### Description

Abort an exchange at the transport level. This action will close connection between fluxzy and client which may lead to depended exchanges to be aborted too.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

abortAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Abort an exchange at the transport level. This action will close connection between fluxzy and client which may lead to depended exchanges to be aborted too.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: AbortAction
    description: Abort
```



### .NET reference

View definition of [AbortAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.AbortAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [abortAction](abortAction)
 - [rejectAction](rejectAction)
 - [rejectWithMessageAction](rejectWithMessageAction)
 - [rejectWithStatusCodeAction](rejectWithStatusCodeAction)
 - [mockedResponseAction](mockedResponseAction)

