using System.Collections.Immutable;
using Microsoft.OpenApi.Models;

namespace Reductech.Sequence.Connectors.Rest;

/// <summary>
/// The REST body parameter
/// </summary>
public class RESTStepBodyParameter : IStepParameter
{
    /// <summary>
    /// Create a new RESTStepBodyParameter
    /// </summary>
    public RESTStepBodyParameter(OpenApiRequestBody requestBody) => RequestBody = requestBody;

    /// <summary>
    /// The Request Body
    /// </summary>
    public OpenApiRequestBody RequestBody { get; }

    /// <inheritdoc />
    public string Name => "Body";

    /// <inheritdoc />
    public Type StepType { get; } = typeof(IStep<>).MakeGenericType(typeof(Entity));

    /// <inheritdoc />
    public Type ActualType { get; } = typeof(Entity);

    /// <inheritdoc />
    public IReadOnlyCollection<string> Aliases => new List<string>();

    /// <inheritdoc />
    public bool Required => RequestBody.Required;

    /// <inheritdoc />
    public string Summary => RequestBody.Description;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ExtraFields => new Dictionary<string, string>();

    /// <inheritdoc />
    public int? Order => null;

    /// <inheritdoc />
    public MemberType MemberType => MemberType.Step;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata =>
        ImmutableDictionary<string, string>.Empty;
}

/// <summary>
/// A security parameter to a REST Step
/// </summary>
public class RESTStepSecurityParameter : IRESTStepParameter
{
    /// <summary>
    /// Create a new RESTStepSecurityParameter
    /// </summary>
    public RESTStepSecurityParameter(OpenApiSecurityScheme openApiSecurityScheme) =>
        OpenApiSecurityScheme = openApiSecurityScheme;

    /// <summary>
    /// The Security Scheme
    /// </summary>
    public OpenApiSecurityScheme OpenApiSecurityScheme { get; }

    /// <inheritdoc />
    public string Name => OpenApiSecurityScheme.Name;

    /// <inheritdoc />
    public Type StepType { get; } = typeof(IStep<>).MakeGenericType(typeof(StringStream));

    /// <inheritdoc />
    public Type ActualType { get; } = typeof(StringStream);

    /// <inheritdoc />
    public IReadOnlyCollection<string> Aliases => ImmutableArray<string>.Empty;

    /// <inheritdoc />
    public bool Required => false;

    /// <inheritdoc />
    public string Summary => OpenApiSecurityScheme.Description;

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> ExtraFields =>
        ImmutableDictionary<string, string>.Empty;

    /// <inheritdoc />
    public int? Order => null;

    /// <inheritdoc />
    public MemberType MemberType => MemberType.Step;

    /// <inheritdoc />
    public string ParameterName => OpenApiSecurityScheme.Name;

    /// <inheritdoc />
    public ParameterLocation? ParameterLocation => OpenApiSecurityScheme.In;

    /// <inheritdoc />
    public object? DefaultValue => "";

    /// <inheritdoc />
    public IReadOnlyDictionary<string, string> Metadata =>
        ImmutableDictionary<string, string>.Empty;
}
