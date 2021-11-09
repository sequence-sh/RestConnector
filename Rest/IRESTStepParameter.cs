using Microsoft.OpenApi.Models;
using Reductech.EDR.Core.Internal;

namespace Reductech.EDR.Connectors.Rest
{

/// <summary>
/// A parameter to a REST step
/// </summary>
public interface IRESTStepParameter : IStepParameter
{
    /// <summary>
    /// The parameter name
    /// </summary>
    string ParameterName { get; }

    /// <summary>
    /// The location of the parameter
    /// </summary>
    ParameterLocation? ParameterLocation { get; }

    /// <summary>
    /// The default Parameter value
    /// </summary>
    object? DefaultValue { get; }
}

}
