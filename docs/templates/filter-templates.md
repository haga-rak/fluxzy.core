## absoluteUriFilter 

### Description 

Select exchanges according to the request absolute URI. 


### Evaluation scope 

**RequestHeaderReceivedFromClient** : The filter is evaluated when the proxy receive the full request header from the client (excluding the body). 

### Yaml configuration name 

    fullUrlFilter


### Settings 

This filter has no settings. 

| Property | Type | Description | Default value |
| :-------- | :---- | :----------- | ------- |
| inverted | boolean | negate the filter result | false |
| pattern  | string | The string pattern to search | *none* |
|  stringSelectorOperation | exact \| contains \| regex | The operation performed. | contains | 

### Usage examples 

```yaml
rules:
    filter: 
        typeKind: absoluteUriFilter 
        pattern: ^https\:\/\/   # Select https request 
        stringSelectorOperation: contains 
```

### See also 

PathFilter, HostFilter, AuthorityFilter, QueryStringFilter

------

(to be renamed to AbsoluteUrifilter, UriPathFilter)



