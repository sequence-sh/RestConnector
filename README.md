# Sequence® Rest Connector

[Sequence®](https://gitlab.com/reductech/sequence) is a collection of
libraries that automates cross-application e-discovery and forensic workflows.


The REST Connector allows users to connect to REST services.

This connector works by generating steps from an OpenAPI specification defined in the configuration.

```scala
  "reductech-scl.sequence.connectors": {
    "Reductech.Sequence.Connectors.Rest": {
      "Id": "Reductech.Sequence.Connectors.Rest",
      "Version": "0.13.0",
      "Settings": {
        "Specifications": [{
            "Name": "Examples",
            "BaseURL": "http://baseURL",
            "SpecificationURL": "https://raw.githubusercontent.com/OAI/OpenAPI-Specification/main/examples/v3.0/api-with-examples.json"
          }]
      }
    }
  }
```

Each Specification has the following properties

| Name                  | Required | Type     | Description                              |
| --------------------- | -------- | -------- | ---------------------------------------- |
| Name                  | ✔        | `string` | The name of the Specification            |
| BaseURL               | ✔        | `string` | The base url to send queries to          |
| Specification         |          | `string` | Text of the OpenAPI specification to use |
| SpecificationURL      |          | `string` | Url of the Specification to use          |
| SpecificationFilePath |          | `string` | File path of the Specification to use.   |

Exactly one of Specification, SpecificationURL, and SpecificationFilePath must be set.

Each operation in the specification will map onto its own step. The name of the step will be the Specification name concatenated with the OperationId separated by an underscore. Each operation parameter will map to a Step parameter. Each security definition will map to a Step parameter.

SCL examples available [here](https://docs.reductech.io/edr/examples/rest.html).


# Releases

Can be downloaded from the [Releases page](https://gitlab.com/reductech/edr/connectors/rest/-/releases).

# NuGet Packages

Are available in the [Reductech Nuget feed](https://gitlab.com/reductech/nuget/-/packages).
