using System.Collections.Immutable;
using Microsoft.OpenApi.Models;

namespace Sequence.Connectors.Rest;

/// <summary>
/// A parameter to a REST step
/// </summary>
public class RESTStepParameter : IRESTStepParameter
{
    /// <summary>
    /// Create a new RESTStepParameter
    /// </summary>
    public RESTStepParameter(OpenApiParameter parameter)
    {
        Parameter = parameter;

        ActualType = GetType(parameter.Schema);

        StepType          = typeof(IStep<>).MakeGenericType(ActualType);
        StepTypeReference = TypeReference.Create(ActualType);
    }

    private static Type GetType(OpenApiSchema schema)
    {
        var schemaType   = schema.Type.ToLowerInvariant();
        var schemaFormat = schema.Format?.ToLowerInvariant();

        if (schemaType == "string")
        {
            if (schemaFormat is "date-time" or "date")
                return typeof(SCLDateTime);

            return typeof(StringStream);
        }

        if (schemaType == "double")
            return typeof(SCLDouble);

        if (schemaType == "null")
            return typeof(SCLNull);

        if (schemaType == "integer")
        {
            if (schema.Format == "int32")
                return typeof(SCLInt);

            if (schema.Format == "int64")
                return typeof(SCLInt); //TODO long

            return typeof(SCLInt);
        }

        if (schemaType == "boolean")
            return typeof(SCLBool);

        if (schemaType == "array")
        {
            var memberType = GetType(schema.Items);
            return typeof(Array<>).MakeGenericType(memberType);
        }

        if (schemaType == "object")
        {
            return typeof(Entity);
        }

        throw new Exception(
            $"Cannot get type from schema type: {schema.Type} and format: {schema.Format}"
        );
    }

    /// <summary>
    /// The OpenAPI parameter
    /// </summary>
    public OpenApiParameter Parameter { get; }

    /// <inheritdoc />
    public string Name => Parameter.Name;

    /// <inheritdoc />
    public Type StepType { get; }

    /// <inheritdoc />
    public TypeReference StepTypeReference { get; }

    /// <inheritdoc />
    public Type ActualType { get; }

    /// <inheritdoc />
    public IReadOnlyCollection<string> Aliases => ImmutableArray<string>.Empty;

    /// <inheritdoc />
    public bool Required => Parameter.Required;

    /// <inheritdoc />
    public string Summary => Parameter.Description;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ExtraFields =>
        ImmutableDictionary<string, string>.Empty;

    /// <inheritdoc />
    public int? Order => null;

    /// <inheritdoc />
    public MemberType MemberType => MemberType.Step;

    /// <inheritdoc />
    public string ParameterName => Parameter.Name;

    /// <inheritdoc />
    public ParameterLocation? ParameterLocation => Parameter.In;

    /// <inheritdoc />
    public object? DefaultValue => Parameter.Schema.Default;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata =>
        ImmutableDictionary<string, string>.Empty;
}
