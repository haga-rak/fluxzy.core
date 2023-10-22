## mockedResponseAction

### Description

Reply with a pre-made response from a raw text or file

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

{.alert .alert-info}
:::
**requestHeaderReceivedFromClient** This scope occurs the moment fluxzy parsed the request header receiveid from client
:::

### YAML configuration name

mockedResponseAction

### Settings

The following table describes the customizable properties available for this action: 

{.property-table .property-table-action}
:::
| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| response.statusCode | int32 | The status code of the response |  |
| response.headers | array of (name, value) | Response headers |  |
| response.body.origin | notSet \| fromString \| fromImmediateArray \| fromFile | Defines how the content body should be loaded |  |
| response.body.type | unknown \| text \| json \| xml \| html \| binary \| css \| javaScript \| js \| font \| proto | The body type. Use this property to avoid defining manually `content-type` header.This property is ignored if `Content-Type` is defined explicitly. |  |
| response.body.text | string | When Origin = fromString, the content text to be used as response body. Supports variable. |  |
| response.body.fileName | string | When Origin = fromFile, the path to the file to be used as response body. |  |
| response.body.contentBase64 | string | When Origin = fromImmediateArray, base64 encoded content of the response |  |

:::
### Example of usage

The following examples apply this action to any exchanges

Mock a response with a raw text.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: MockedResponseAction
    response:
      statusCode: 200
      headers:
      - name: DNT
        value: 1
      - name: X-Custom-Header
        value: Custom-HeaderValue
      body:
        origin: FromString
        type: Json
        text: '{ "result": true }'
```


Mock a response with a file.

```yaml
rules:
- filter:
    typeKind: AnyFilter
  actions:
  - typeKind: MockedResponseAction
    response:
      statusCode: 404
      headers:
      - name: Server
        value: Fluxzy
      - name: X-Custom-Header-2
        value: Custom-HeaderValue-2
      body:
        origin: FromFile
        type: Binary
        fileName: /path/to/my/response.data
```



### See also

This action has no related action

