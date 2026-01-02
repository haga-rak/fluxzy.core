## applySessionAction

### Description

Apply captured session data to requests. Adds cookies from session store and optionally applies stored headers. Works in conjunction with CaptureSessionAction.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

applySessionAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Apply session cookies to requests.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ApplySessionAction
    applyCookies: true
    mergeWithExisting: true
```


Apply all session data (cookies and headers).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ApplySessionAction
    applyCookies: true
    applyHeaders: true
    mergeWithExisting: true
```


Apply session from a specific domain.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ApplySessionAction
    applyCookies: true
    applyHeaders: true
    mergeWithExisting: true
    sourceDomain: auth.example.com
```


Replace existing cookies entirely with session cookies.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: ApplySessionAction
    applyCookies: true
    applyHeaders: true
```



### .NET reference

View definition of [ApplySessionAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.ApplySessionAction.html) for .NET integration.

### See also

This action has no related action

