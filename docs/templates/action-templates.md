## addRequestHeaderAction 

### Description 

Append a request header. 


### Evaluation scope 

**RequestHeaderReceivedFromClient** : The filter is evaluated when the proxy receive the full request header from the client (excluding the body). 

### Yaml configuration name 

    addRequestHeaderAction


### Settings 

This filter has no settings. 

| Property | Type | Description | Default value |
| :-------- | :---- | :----------- | ------- |
| headerName  | string |  | *none* |
| headerValue  | string |  | *none* |

### Usage examples 

```yaml
rules:
    filter: 
        typeKind: anyFilter 
        pattern: ^https\:\/\/   # Select https request 
        stringSelectorOperation: contains 
	action: 
		typeKind: addRequestHeaderAction
		headerName: "Authorization" 
		headerValue: "Bearer my_special_token"
```

### See also 

PathFilter, HostFilter, AuthorityFilter, QueryStringFilter

------

(to be renamed to AbsoluteUrifilter, UriPathFilter)



