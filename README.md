# Sequence REST Connector

[Sequence®](https://sequence.sh) is a collection of libraries for
automation of cross-application e-discovery and forensic workflows.

The REST Connector enables users to interact with REST services in sequences.

This connector works by generating steps from an OpenAPI specification defined in the configuration.

```json
  "Sequence.Connectors.Rest": {
    "Sequence.Connectors.Rest": {
      "Id": "Sequence.Connectors.Rest",
      "Version": "0.18.0",
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

SCL examples available [here](https://sequence.sh/docs/examples/connectors/rest/).

# Documentation

https://sequence.sh

# Download

https://sequence.sh/download

# Try SCL and Core

https://sequence.sh/playground

# Package Releases

Can be downloaded from the [Releases page](https://gitlab.com/sequence/connectors/rest/-/releases).

# NuGet Packages

Release nuget packages are available from [nuget.org](https://www.nuget.org/profiles/Sequence).
