using System.Collections.Immutable;
using Microsoft.OpenApi.Models;

namespace Reductech.Sequence.Connectors.Rest;

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
        Parameter  = parameter;
        ActualType = typeof(StringStream);
        StepType   = typeof(IStep<>).MakeGenericType(ActualType);
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
}
