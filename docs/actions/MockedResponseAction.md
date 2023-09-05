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
| response.bodyContent.origin | fromString \| fromImmediateArray \| fromFile | Defines how to load content body |  |
| response.bodyContent.mime | string | Mime. Example = 'application/json' |  |
| response.bodyContent.text | string | When Origin = fromString, the content text to be used as response body |  |
| response.bodyContent.fileName | string | When Origin = fromFile, the path to the file to be used as response body |  |
| response.bodyContent.content | byte[] | When Origin = fromImmediateArray, base64 encoded content of the response |  |
| response.bodyContent.headers | dictionary`2 | Key values containing extra headers |  |

:::
### Example of usage

This filter has no specific usage example


