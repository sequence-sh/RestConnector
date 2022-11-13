using Sequence.ConnectorManagement.Base;
using Sequence.Core.Abstractions;
using Sequence.Core.Internal.Errors;

namespace Sequence.Connectors.Rest;

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

        var sclObject = ISCLObject.CreateFromCSharpObject(valueObject);

        var specifications = new List<OpenAPISpecification>();

        if (sclObject is Entity entity)
        {
            var r = EntityConversionHelpers.TryCreateFromEntity<OpenAPISpecification>(
                entity
            );

            if (r.IsSuccess)
                specifications.Add(r.Value);
        }
        else if (sclObject is IArray array)
        {
            foreach (var value in array.ListIfEvaluated().Value.OfType<Entity>())
            {
                var r = EntityConversionHelpers.TryCreateFromEntity<OpenAPISpecification>(value);

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
