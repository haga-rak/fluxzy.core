## setResponseCookieAction

### Description

Add a response cookie. This action is performed by adding `Set-Cookie` header in response.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**responseHeaderReceivedFromRemote** This scope occurs the moment fluxzy has done parsing the response header.
:::

### YAML configuration name

setResponseCookieAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| name | string | Cookie name |  |
| value | string | Cookie value |  |
| path | string | Cookie path |  |
| domain | string | Cookie domain |  |
| expireInSeconds | integer | Cookie expiration date in seconds from now |  |
| maxAge | integer | Cookie max age in seconds |  |
| httpOnly | boolean | HttpOnly property | false |
| secure | boolean | Secure property | false |
| sameSite | string | Set `SameSite` property. Usual values are Strict, Lax and None. [Check](https://developer.mozilla.org/docs/Web/HTTP/Headers/Set-Cookie)  |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Set a cookie with name `my-cookie` and value `my-value`.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetResponseCookieAction
    name: my-cookie
    value: my-value
```


Add cookie with all properties.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: SetResponseCookieAction
    name: my-cookie
    value: my-value
    path: /
    domain: example.com
    expireInSeconds: 3600
    maxAge: 3600
    httpOnly: true
    secure: true
    sameSite: Strict
```



### See also

The following actions are related to this action: 

 - [setRequestCookieAction](setRequestCookieAction)
 - [setResponseCookieAction](setResponseCookieAction)

