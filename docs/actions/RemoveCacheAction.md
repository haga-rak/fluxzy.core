## removeCacheAction

### Description

Remove all cache directive from request and response headers. This will force the clientto ask the latest version of the requested resource.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client

### YAML configuration name

    removeCacheAction

### Settings

This action has no specific characteristic

### Example of usage

The following examples apply this action to any exchanges

Remove all cache directive from request and response headers. This will force the clientto ask the latest version of the requested resource.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: RemoveCacheAction
```



