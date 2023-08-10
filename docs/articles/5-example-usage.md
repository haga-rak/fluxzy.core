
# Startup example

## A capture session with the CLI 

**Note**: The command line tool has a complete help when run with the `-h`option. The following examples showcase only how to quick start with the CLI. 

Once you installed the command line tool and set into your working directory, you can run the following command line to start fxzy

```Bash
  fxzy start 
```
  
This will make fluxzy listen to any localhost address (IPV4 and IPV6) on the port 44344. A this point, nothing special will happen until you deflect request into the proxy. 
To confirm that everything is fine, you can run the following request 

```Bash
curl -X 127.0.0.1:44344 -i http://fluxzy.io/check 
```

This request will return 200 normaly. 



You will find below few example of fluxzy usage. For this, we're gonna use the command line tool (fxzy) :

##  Injecting a bearer token 

In this example, we have clients that want to make a request to a particular server. This later requires a bearer token that the clients don't possess. 
We're gonna use fluxzy to inject a bearer token when some request to the server is made. 


```yaml 
rules:
  - filter:
      typeKind: Anykind 
    actions: 
      - typeKind: AddResponseHeaderAction
        headerName: Authorization 
        headerValue: Bearer our_secret_token 

``` 









A rule is a combination of a filter and multiple actions. 




A filter is always defined inside a FilterScope. A FilterScope is a particular timing during an HTTP exchange 



For example, to remove any cookies from a subdomain, you can setup the following filt




