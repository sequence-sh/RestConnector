# v0.16.0 (2022-07-13)

Maintenance release - dependency updates only.

# v0.15.0 (2022-05-27)

Maintenance release - dependency updates only.

# v0.14.0 (2022-03-25)

## Summary of Changes

### Steps

The following steps have been moved from Core:

- RESTDelete
- RESTGetJson
- RESTGetStream
- RESTPatch
- RESTPost
- RESTPut

## Issues Closed in this Release

### New Features

- Move Rest Steps from Core into Rest Connector #45

# v0.13.0 (2022-01-16)

EDR is now Sequence. The following has changed:

- The GitLab group has moved to https://gitlab.com/reductech/sequence
- The root namespace is now `Reductech.Sequence`
- The documentation site has moved to https://sequence.sh

Everything else is still the same - automation, simplified.

The project has now been updated to use .NET 6.

## Summary of Changes

It's now possible to define aliases for steps in the REST connector
configuration. For example:

```json
"Aliases" : {
  "Document_Post" : "GetDocument"
}
```

## Issues Closed in this Release

### New Features

- Allow aliases for steps in configuration #25
- Generated steps should have the correct parameter types #24

### Maintenance

- Update license file and add notice #27
- Rename EDR to Sequence #19
- Update Core to support SCLObject types #16
- Upgrade to use .net 6 #15

# v0.12.0 (2021-11-26)

Initial release. Version numbers are aligned with [Core](https://gitlab.com/reductech/edr/core/-/releases).

## Summary of Changes

### Steps

- Added `IDynamicStepGenerator` which allows connectors to generate steps dynamically based on configuration

## Issues Closed in this Release

### New Features

- Enable Security Parameters #4
- Create an IDynamicStepGenerator to generate steps based on an OpenAPI Schema #2

### Maintenance

- Use Text.Json Serializer for requests #6
- Run Rename Script #1

### Other

- Create more tests for REST connector #5



