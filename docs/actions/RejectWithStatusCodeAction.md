## rejectWithStatusCodeAction

### Description

Block the request and return a custom HTTP error response. Allows specifying the status code (e.g., 403, 404, 502) to return to the client. The response body will contain the standard reason phrase for the status code.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

rejectWithStatusCodeAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Block with 404 Not Found (hide resource existence).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RejectWithStatusCodeAction
    statusCode: 404
```


Block with 502 Bad Gateway (simulate server unavailability).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RejectWithStatusCodeAction
    statusCode: 502
```



### .NET reference

View definition of [RejectWithStatusCodeAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.RejectWithStatusCodeAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [abortAction](abortAction)
 - [rejectAction](rejectAction)
 - [rejectWithMessageAction](rejectWithMessageAction)
 - [rejectWithStatusCodeAction](rejectWithStatusCodeAction)
 - [mockedResponseAction](mockedResponseAction)

