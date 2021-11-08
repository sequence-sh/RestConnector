using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.OpenApi.Models;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Internal;

namespace Reductech.EDR.Connectors.Rest
{

/// <summary>
/// A security parameter to a REST Step
/// </summary>
public class RESTStepSecurityParameter : IRESTStepParameter
{
    /// <summary>
    /// Create a new RESTStepSecurityParameter
    /// </summary>
    public RESTStepSecurityParameter(OpenApiSecurityScheme openApiSecurityScheme)
    {
        OpenApiSecurityScheme = openApiSecurityScheme;
        ActualType            = typeof(StringStream);
        StepType              = typeof(IStep<>).MakeGenericType(ActualType);
    }

    /// <summary>
    /// The Security Scheme
    /// </summary>
    public OpenApiSecurityScheme OpenApiSecurityScheme { get; }

    /// <inheritdoc />
    public string Name => OpenApiSecurityScheme.Name;

    /// <inheritdoc />
    public Type StepType { get; }

    /// <inheritdoc />
    public Type ActualType { get; }

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
}

}
