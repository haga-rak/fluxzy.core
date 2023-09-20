## noOpAction

### Description

An action doing no operation.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestBodyReceivedFromClient** This scope occurs the moment fluxzy received fully the request body from the client. In a fullstreaming mode which is the default mode, this event occurs when the full body is already fully sent to the remote server. 
:::

### YAML configuration name

noOpAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

An action doing no operation.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: NoOpAction
    description: No operation
    noEditableSetting: true
```



