## rejectWithMessageAction

### Description

Block the request with a custom HTTP error response including a body message. Useful for providing detailed blocking reasons to end users. Supports text/plain, text/html, and application/json content types.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

rejectWithMessageAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Block with a plain text message.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RejectWithMessageAction
    statusCode: 403
    message: Access to this site is blocked by corporate policy
    contentType: text/plain
```


Block with an HTML page.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RejectWithMessageAction
    statusCode: 403
    message: <html><body><h1>Blocked</h1><p>This site has been blocked for security reasons.</p></body></html>
    contentType: text/html
```


Block with a JSON response (for APIs).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RejectWithMessageAction
    statusCode: 403
    message: '{"error": "forbidden", "message": "This endpoint is blocked"}'
    contentType: application/json
```



### .NET reference

View definition of [RejectWithMessageAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.RejectWithMessageAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [abortAction](abortAction)
 - [rejectAction](rejectAction)
 - [rejectWithMessageAction](rejectWithMessageAction)
 - [rejectWithStatusCodeAction](rejectWithStatusCodeAction)
 - [mockedResponseAction](mockedResponseAction)

