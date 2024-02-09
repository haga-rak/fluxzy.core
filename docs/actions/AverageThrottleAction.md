## averageThrottleAction

### Description

Throttle and simulate bandwidth condition.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

averageThrottleAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Throttle and simulate bandwidth condition.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: AverageThrottleAction
    bandwidthBytesPerSeconds: 65536
    throttleChannel: All
```



### .NET reference

View definition of [AverageThrottleAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.AverageThrottleAction.html) for .NET integration.

### See also

This action has no related action

