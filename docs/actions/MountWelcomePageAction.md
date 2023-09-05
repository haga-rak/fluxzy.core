## mountWelcomePageAction

### Description

Reply with fluxzy welcome page

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

mountWelcomePageAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Reply with fluxzy welcome page.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: MountWelcomePageAction
    noEditableSetting: true
```



