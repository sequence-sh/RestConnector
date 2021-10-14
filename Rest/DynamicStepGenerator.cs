using System.Collections.Generic;
using System.Linq;
using Microsoft.OpenApi.Readers;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;

namespace Reductech.EDR.Connectors.Rest
{

/// <summary>
/// Generates steps based on OpenAPI step definitions 
/// </summary>
public class DynamicStepGenerator : IDynamicStepGenerator
{
    /// <inheritdoc />
    public IEnumerable<IStepFactory> CreateStepFactories(ConnectorSettings connectorSettings)
    {
        if (connectorSettings.Settings is null)
            yield break;

        if (connectorSettings.Settings.TryGetValue(SpecificationsKey, out var v))
        {
            var entityValue = EntityValue.CreateFromObject(v);

            var specifications = new List<OpenAPISpecification>();

            if (entityValue is EntityValue.NestedEntity nestedEntity)
            {
                var r = EntityConversionHelpers.TryCreateFromEntity<OpenAPISpecification>(
                    nestedEntity.Value
                );

                if (r.IsSuccess)
                    specifications.Add(r.Value);
            }
            else if (entityValue is EntityValue.NestedList nestedList)
            {
                foreach (var value in nestedList.Value.OfType<EntityValue.NestedEntity>())
                {
                    var r = EntityConversionHelpers.TryCreateFromEntity<OpenAPISpecification>(
                        value.Value
                    );

                    if (r.IsSuccess)
                        specifications.Add(r.Value);
                }
            }

            foreach (var stepFactory in specifications.SelectMany(CreateStepFactories))
                yield return stepFactory;
        }
    }

    /// <summary>
    /// The key for OpenAPI specifications in the Settings
    /// </summary>
    public const string SpecificationsKey = "Specifications";

    /// <summary>
    /// Create Step Factories from an OpenAPI specification
    /// </summary>
    public static IEnumerable<IStepFactory> CreateStepFactories(OpenAPISpecification specification)
    {
        var stream = new StringStream(specification.Specification);

        var openApiDocument = new OpenApiStreamReader().Read(
            stream.GetStream().stream,
            out _
        );

        if (openApiDocument is null || openApiDocument.Paths is null)
            yield break;

        foreach (var (path, pathItem) in openApiDocument.Paths)
        foreach (var (operationType, openApiOperation) in pathItem.Operations)
        {
            var metadata = new OperationMetadata(
                specification.Name,
                openApiDocument,
                path,
                specification.BaseURL,
                pathItem,
                openApiOperation,
                operationType
            );

            yield return new RESTStepFactory(metadata);
        }
    }
}

}
