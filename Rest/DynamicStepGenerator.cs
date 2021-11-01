using System.Collections.Generic;
using System.Linq;
using CSharpFunctionalExtensions;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;

namespace Reductech.EDR.Connectors.Rest
{

/// <summary>
/// Generates steps based on OpenAPI step definitions 
/// </summary>
public class DynamicStepGenerator : IDynamicStepGenerator
{
    /// <inheritdoc />
    public Result<IReadOnlyList<IStepFactory>, IErrorBuilder> TryCreateStepFactories(
        ConnectorSettings connectorSettings,
        IExternalContext externalContext)
    {
        if (connectorSettings.Settings is null)
            return Result.Success<IReadOnlyList<IStepFactory>, IErrorBuilder>(
                new List<IStepFactory>()
            );

        if (!connectorSettings.Settings.TryGetValue(SpecificationsKey, out var valueObject))
        {
            return Result.Success<IReadOnlyList<IStepFactory>, IErrorBuilder>(
                new List<IStepFactory>()
            );
        }

        var entityValue = EntityValue.CreateFromObject(valueObject);

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

        var stepFactories = specifications.Select(
                x =>
                    x.TryGetStepFactories(externalContext)
            )
            .Combine(ErrorBuilderList.Combine)
            .Map(x => x.SelectMany(y => y).ToList() as IReadOnlyList<IStepFactory>);

        return stepFactories;
    }

    /// <summary>
    /// The key for OpenAPI specifications in the Settings
    /// </summary>
    public const string SpecificationsKey = "Specifications";
}

}
