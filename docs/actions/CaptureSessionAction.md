## captureSessionAction

### Description

Capture session data from responses. Captures Set-Cookie headers and optionally other headers like Authorization. Can also capture cookies from request headers for intercepting ongoing sessions. Stored data can be replayed using ApplySessionAction.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

captureSessionAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Capture cookies from responses.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: CaptureSessionAction
    captureCookies: true
    captureHeaders: []
```


Capture cookies and Authorization header.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: CaptureSessionAction
    captureCookies: true
    captureHeaders:
    - Authorization
```


Capture cookies and multiple custom headers.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: CaptureSessionAction
    captureCookies: true
    captureHeaders:
    - Authorization
    - X-CSRF-Token
    - X-Auth-Token
```


Capture cookies from request headers (for ongoing sessions).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: CaptureSessionAction
    captureCookies: true
    captureRequestCookies: true
    captureHeaders: []
```



### .NET reference

View definition of [CaptureSessionAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.HighLevelActions.CaptureSessionAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [applySessionAction](applySessionAction)
 - [captureSessionAction](captureSessionAction)
 - [clearSessionAction](clearSessionAction)

