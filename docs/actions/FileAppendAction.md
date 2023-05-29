## fileAppendAction

### Description

Write to a file. Captured variable are interpreted.

### Evaluation scope

Evaluation scope defines the timing where this filter will be applied. 

**outOfScope** Means that the filter or action associated to this scope won't be trigger in the regular HTTP flow. This scope is applied only on view filter and internat actions.

### YAML configuration name

    fileAppendAction

### Settings

The following table describes the customizable properties available for this filter: 

| Property | Type | Description | DefaultValue |
| :------- | :------- | :------- | -------- |
| filename | string | Filename |  |
| text | string | Text to write |  |
| encoding | string | Default encoding. UTF-8 if not any. | *null* |
| runScope | nullable`1 | When RunScope is defined. The action is only evaluated when the value of the scope occured. | *null* |

### Example of usage

This filter has no specific usage example


