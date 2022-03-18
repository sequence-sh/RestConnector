using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Reductech.Sequence.Core.Internal.Errors;
using Reductech.Sequence.Core.Internal.Serialization;
using Reductech.Sequence.Core.Steps.REST;
using RestSharp;

namespace Reductech.Sequence.Connectors.Rest;

/// <summary>
/// A REST step that has been dynamically generated from an OpenAPI schema
/// </summary>
[NotAStaticStep]
public sealed class RESTDynamicStep<T> : IStep<T> where T : ISCLObject
{
    /// <summary>
    /// Create a new RestDynamicStep
    /// </summary>
    public RESTDynamicStep(
        OperationMetadata operationMetadata,
        Func<string, Result<T, IErrorBuilder>> convertResultFunc,
        IReadOnlyList<(IStep step, IRESTStepParameter restStepParameter)> allParameters,
        Maybe<(IStep<Entity>? step, RESTStepBodyParameter parameter)> bodyParameter,
        TextLocation? textLocation)
    {
        OperationMetadata = operationMetadata;
        ConvertResultFunc = convertResultFunc;
        AllParameters     = allParameters;
        BodyParameter     = bodyParameter;
        TextLocation      = textLocation;
    }

    /// <inheritdoc />
    public string Name => OperationMetadata.Name;

    /// <summary>
    /// REST Parameters and their values as steps
    /// </summary>
    public IReadOnlyList<(IStep step, IRESTStepParameter restStepParameter)> AllParameters { get; }

    /// <summary>
    /// The Body parameter
    /// </summary>
    public Maybe<(IStep<Entity>? step, RESTStepBodyParameter parameter)> BodyParameter { get; }

    /// <inheritdoc />
    public TextLocation? TextLocation { get; set; }

    /// <summary>
    /// The operation metadata
    /// </summary>
    public OperationMetadata OperationMetadata { get; }

    /// <summary>
    /// Function to convert the result to the output type
    /// </summary>
    public Func<string, Result<T, IErrorBuilder>> ConvertResultFunc { get; }

    /// <inheritdoc />
    public async Task<Result<T, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var errors          = new List<IError>();
        var parameterValues = new List<(IRESTStepParameter parameter, string value)>();

        foreach (var (step, restStepParameter) in AllParameters)
        {
            var r = await step.RunUntyped(stateMonad, cancellationToken)
                .Map(x => x.Serialize(SerializeOptions.Primitive));

            if (r.IsFailure)
                errors.Add(r.Error);
            else
                parameterValues.Add((restStepParameter, r.Value));
        }

        if (errors.Any())
            return Result.Failure<T, IError>(ErrorList.Combine(errors));

        var method = OperationMetadata.OperationType switch
        {
            OperationType.Get => Method.GET,
            OperationType.Put => Method.PUT,
            OperationType.Post => Method.POST,
            OperationType.Delete => Method.DELETE,
            OperationType.Options => Method.OPTIONS,
            OperationType.Head => Method.HEAD,
            OperationType.Patch => Method.PATCH,
            _ => throw new ArgumentOutOfRangeException(OperationMetadata.OperationType.ToString())
        };

        IRestRequest request = new RestRequest(OperationMetadata.Path, method);

        var restClient =
            stateMonad.ExternalContext.RestClientFactory.CreateRestClient(
                OperationMetadata.ServerUrl
            );

        if (BodyParameter.HasValue && BodyParameter.Value.step is not null)
        {
            var bodyResult = await BodyParameter.Value.step.Run(stateMonad, cancellationToken);

            if (bodyResult.IsFailure)
                return bodyResult.ConvertFailure<T>();

            var jsonElement = bodyResult.Value.ToJsonElement();
            var obj         = JsonSerializer.Deserialize<object>(jsonElement.GetRawText())!;

            request.AddJsonBody(obj);
        }

        foreach (var (parameter, value) in parameterValues)
        {
            var parameterType = parameter.ParameterLocation switch
            {
                ParameterLocation.Query => ParameterType.QueryString,
                ParameterLocation.Header => ParameterType.HttpHeader,
                ParameterLocation.Path => ParameterType.UrlSegment,
                ParameterLocation.Cookie => ParameterType.Cookie,
                null => ParameterType.Cookie,
                _ => throw new ArgumentOutOfRangeException(parameter.ParameterLocation?.ToString())
            };

            request = request.AddParameter(parameter.ParameterName, value, parameterType);
        }

        var resultString =
            await request.TryRun(restClient, cancellationToken);

        if (resultString.IsFailure)
            return resultString.ConvertFailure<T>().MapError(x => x.WithLocation(this));

        var result = GetResult(resultString.Value).MapError(x => x.WithLocation(this));

        return result;
    }

    /// <summary>
    /// Create the result from the output string
    /// </summary>
    public Result<T, IErrorBuilder> GetResult(string s) => ConvertResultFunc(s);

    /// <inheritdoc />
    public Result<Unit, IError> Verify(StepFactoryStore stepFactoryStore)
    {
        var r3 = AllParameters
            .Select(x => x.step.Verify(stepFactoryStore));

        var finalResult = r3
            .Combine(ErrorList.Combine)
            .Map(_ => Unit.Default);

        return finalResult;
    }

    /// <inheritdoc />
    public Task<Result<T1, IError>> Run<T1>(
        IStateMonad stateMonad,
        CancellationToken cancellationToken) where T1 : ISCLObject
    {
        return Run(stateMonad, cancellationToken)
            .BindCast<T, T1, IError>(
                ErrorCode.InvalidCast.ToErrorBuilder(typeof(T), typeof(T1)).WithLocation(this)
            );
    }

    /// <inheritdoc />
    public Task<Result<ISCLObject, IError>> RunUntyped(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        return Run(stateMonad, cancellationToken).Map(x => x as ISCLObject);
    }

    /// <inheritdoc />
    public string Serialize(SerializeOptions options)
    {
        var sb = new StringBuilder();
        sb.Append(Name);

        foreach (var (step, parameter) in AllParameters)
        {
            sb.Append(' ');

            sb.Append(parameter.Name);
            sb.Append(": ");

            var value = step.Serialize(options);

            sb.Append(value);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public void Format(
        IndentationStringBuilder indentationStringBuilder,
        FormattingOptions options,
        Stack<Comment> remainingComments)
    {
        var serializer = new FunctionSerializer(Name);

        IEnumerable<StepProperty> stepProperties
            = AllParameters.Select(
                (x, i) =>
                    new StepProperty.SingleStepProperty(
                        x.step,
                        x.restStepParameter,
                        i,
                        null,
                        ImmutableList<RequirementAttribute>.Empty
                    )
            );

        serializer.Format(
            stepProperties,
            TextLocation,
            indentationStringBuilder,
            options,
            remainingComments
        );
    }

    /// <inheritdoc />
    public bool ShouldBracketWhenSerialized => true;

    /// <inheritdoc />
    public Type OutputType => typeof(Entity);

    /// <inheritdoc />
    public IEnumerable<Requirement> RuntimeRequirements
    {
        get
        {
            yield break;
        }
    }

    /// <inheritdoc />
    public bool HasConstantValue(IEnumerable<VariableName> providedVariables) => false;

    /// <inheritdoc />
    public Task<Maybe<ISCLObject>> TryGetConstantValueAsync(
        IReadOnlyDictionary<VariableName, ISCLObject> variableValues,
        StepFactoryStore sfs)
    {
        return Task.FromResult(Maybe<ISCLObject>.None);
    }

    /// <inheritdoc />
    public IEnumerable<(IStep Step, IStepParameter Parameter, IStep Value)> GetParameterValues()
    {
        foreach (var (nestedStep, parameter) in this.AllParameters)
        {
            yield return (this, parameter, nestedStep);

            foreach (var parameterValue in nestedStep.GetParameterValues())
                yield return parameterValue;
        }
    }
}
