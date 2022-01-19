# Sequence® Rest Connector

[Sequence®](https://gitlab.com/reductech/sequence) is a collection of
libraries that automates cross-application e-discovery and forensic workflows.

This connector contains Steps to interact with...

## Steps

|         Step          | Description                                    | Result Type |
| :-------------------: | :--------------------------------------------- | :---------: |
| `ConvertJsonToEntity` | Converts a JSON string or stream to an entity. |  `Entity`   |

## Examples

To check if a file exists and print the result:

```scala
- Print (ConvertJsonToEntity '{"Foo":1}')
```

# Releases

Can be downloaded from the [Releases page](https://gitlab.com/reductech/edr/connectors/rest/-/releases).

# NuGet Packages

Are available in the [Reductech Nuget feed](https://gitlab.com/reductech/nuget/-/packages).
