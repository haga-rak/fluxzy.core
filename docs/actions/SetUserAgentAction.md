## setUserAgentAction

### Description

Change the User-AgentThis action is used to change the User-Agent header of the request from a list of built-in user-agent values.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

setUserAgentAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string |  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Change `User-Agent` to `Windows_Chrome` (`Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: Windows_Chrome
```


Change `User-Agent` to `Windows_Firefox` (`Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:125.0) Gecko/20100101 Firefox/125.0`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: Windows_Firefox
```


Change `User-Agent` to `Windows_Edge` (`Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.11.3`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: Windows_Edge
```


Change `User-Agent` to `macOS_Chrome` (`Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: macOS_Chrome
```


Change `User-Agent` to `macOS_Firefox` (`Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:125.0) Gecko/20100101 Firefox/125.0`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: macOS_Firefox
```


Change `User-Agent` to `macOS_Safari` (`Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/14.1.2 Safari/605.1.15`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: macOS_Safari
```


Change `User-Agent` to `macOS_Edge` (`Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36 Edg/124.0.0.11.3`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: macOS_Edge
```


Change `User-Agent` to `Ubuntu_Chrome` (`Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/123.0.0.0 Safari/537.36`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: Ubuntu_Chrome
```


Change `User-Agent` to `Ubuntu_Firefox` (`Mozilla/5.0 (X11; Ubuntu; Linux x86_64; rv:122.0) Gecko/20100101 Firefox/122.0`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: Ubuntu_Firefox
```


Change `User-Agent` to `Ubuntu_Edge` (`Mozilla/5.0 (Wayland; Linux x86_64; System76 Galago Pro (galp2)) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Ubuntu/24.04 Edg/122.0.2365.92`).

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetUserAgentAction
    name: Ubuntu_Edge
```



### .NET reference

View definition of [SetUserAgentAction](https://docs.fluxzy.io/api/Fluxzy.Rules.Actions.SetUserAgentAction.html) for .NET integration.

### See also

The following actions are related to this action: 

 - [addRequestHeaderAction](addRequestHeaderAction)
 - [addResponseHeaderAction](addResponseHeaderAction)
 - [updateRequestHeaderAction](updateRequestHeaderAction)
 - [updateResponseHeaderAction](updateResponseHeaderAction)
 - [deleteRequestHeaderAction](deleteRequestHeaderAction)
 - [deleteResponseHeaderAction](deleteResponseHeaderAction)
 - [setUserAgentAction](setUserAgentAction)

