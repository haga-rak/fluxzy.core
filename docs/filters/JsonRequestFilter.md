## jsonRequestFilter

### Description

Select request sending JSON body. Filtering is made by inspecting value of `Content-Type` header

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**requestBodyReceivedFromClient** This scope occurs the moment fluxzy received fully the request body from the client. In a fullstreaming mode which is the default mode, this event occurs when the full body is already fully sent to the remote server. 

### YAML configuration name

    jsonRequestFilter

### Settings

This filter has no specific characteristic

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| inverted | boolean | Negate the filter result | false |

### Example of usage

The following examples apply a comment to the filtered exchange

Select exchanges having request header `dnt: 1`.

```yaml
rules:
- filter:
    typeKind: RequestHeaderFilter
    headerName: dnt
    pattern: 1
    operation: Exact
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```


Select exchanges issued by Chrome 112 by checking User-Agent.

```yaml
rules:
- filter:
    typeKind: RequestHeaderFilter
    headerName: User-Agent
    pattern: 'Chrome/112 '
    operation: Contains
  actions:
  - typeKind: ApplyCommentAction
    comment: filter was applied
```



