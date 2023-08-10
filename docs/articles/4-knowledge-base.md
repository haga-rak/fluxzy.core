# Knowledge base

fluxzy is an intermediate HTTP agent that acts as MAN on the middle. It uses a combination of filter and action to collect network datas and to process alteration. 
A combination of a filter and multiple actions is called rule. 

## Filter 
A filter is a particular configuration that isolate a special HTTP traffic. The selected result can be used later for displaying purpose or for applying alteration rule named "action".

## Action 
An action is anything one can operate to modify the behaviour of an HTTP exchange. It can be an alteration on HTTP level, such as adding extra HTTP header, to a transport configuration modification, such as forcing an IP address resolution. 

A fluxzy `action` is not limited to the current HTTP context and can interoperate with external tools via process exeuction. 

## Filter Scope 

A `FilterScope` is a particular timing where a `Filter` or an `Action` can be evaluated.  It's mechanism to ensure coherency between those two. For example, if you want to change a response header, the response header must have been received in order to perform the alteration. 

When this rule is violated, you will have a FilterOutScope error at some point. 

Fluxzy supports the main following filterscope: 

- **OnAuthorityReceived** : This scope denotes the moment fluxzy is aware the destination authority
- **RequestHeaderReceivedFromClient** : This scope occurs the moment fluxzy parsed the request header receiveid from client
- **RequestBodyReceivedFromClient** : This scope occurs the moment fluxzy received fully the request body from the client
- **ResponseHeaderReceivedFromRemote** : This scope occurs the moment fluxzy has done parsing the response header.
- **ResponseBodyReceivedFromRemote** : This scope occurs the moment fluxzy received the the response body from the server.

:::{.alert .alert-warning}

*Warning* : **RequestBodyReceivedFromClient** and **ResponseBodyReceivedFromRemote** are per default not triggered at all. Unless a mock is set, fluxzy completely stream request and response body meaning that at the time these event should occured, the actual body content is already sent to the other side.  

:::



## Evaluation order 

filter and Action are evaluated as it appeared. 

## Searching for a fitler and action 

The exhaustive list of `Filter` and `Action` are available under the   [search rule page](resources/search).
