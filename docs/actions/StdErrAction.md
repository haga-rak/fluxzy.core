## stdErrAction

### Description

Write text to standard error. Captured variable are interpreted.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**outOfScope** Means that the filter or action associated to this scope won't be trigger in the regular HTTP flow. This scope is applied only on view filter and internat actions.

### YAML configuration name

    stdErrAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| text | string |  |  |
| runScope | nullable`1 | When RunScope is defined. The action is only evaluated when the value of the scope occured. | *null* |

### Example of usage

This filter has no specific usage example


