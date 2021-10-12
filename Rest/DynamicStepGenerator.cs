using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Newtonsoft.Json;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Steps.REST;
using Reductech.EDR.Core.Util;
using RestSharp;
using Entity = Reductech.EDR.Core.Entity;

namespace Reductech.EDR.Connectors.Rest
{

/// <summary>
/// Helper methods for REST steps
/// </summary>
public static class Helpers
{
    /// <summary>
    /// Creates the name of the step
    /// </summary>
    public static string CreateStepName(
        string serviceTitle,
        string path,
        OperationType operationType)
    {
        return new string(
            (serviceTitle + path + operationType).Where(char.IsLetterOrDigit).ToArray()
        );
    }
}

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
            foreach (var stepFactory in CreateStepFactories(v.ToString()!))
                yield return stepFactory;
        }
    }

    /// <summary>
    /// The key for OpenAPI specifications in the Settings
    /// </summary>
    public const string SpecificationsKey = "Specifications";

    public static IEnumerable<IStepFactory> CreateStepFactories(string specification)
    {
        var stream = new StringStream(specification);

        var openApiDocument = new OpenApiStreamReader().Read(
            stream.GetStream().stream,
            out _
        );

        if (!openApiDocument.Servers.Any())
            yield break;

        var server = openApiDocument.Servers.First();

        foreach (var (path, pathItem) in openApiDocument.Paths)
        foreach (var (operationType, openApiOperation) in pathItem.Operations)
            if (operationType == OperationType.Get)
                yield return new RESTGetStepFactory(
                    openApiDocument,
                    path,
                    server,
                    pathItem,
                    openApiOperation
                );
    }
}

[NotAStaticStep]
public sealed class RESTDynamicGet : IStep<Entity>
{
    public RESTDynamicGet(
        OpenApiDocument document,
        string path,
        OpenApiServer server,
        OpenApiPathItem pathItem,
        OpenApiOperation operation,
        IReadOnlyList<(IStep step, RESTStepParameter restStepParameter)> allProperties,
        TextLocation? textLocation)
    {
        Document      = document;
        Path          = path;
        Server        = server;
        PathItem      = pathItem;
        Operation     = operation;
        AllProperties = allProperties;
        TextLocation  = textLocation;
    }

    /// <inheritdoc />
    public string Name => Path.TrimStart('/') + OperationType;

    public OpenApiDocument Document { get; }
    public string Path { get; }
    public OpenApiServer Server { get; }
    public OpenApiPathItem PathItem { get; }
    public OpenApiOperation Operation { get; }

    public IReadOnlyList<(IStep step, RESTStepParameter restStepParameter)> AllProperties { get; }

    /// <inheritdoc />
    public TextLocation? TextLocation { get; set; }

    public OperationType OperationType { get; } = OperationType.Get;

    /// <inheritdoc />
    public async Task<Result<Entity, IError>> Run(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        var errors          = new List<IError>();
        var parameterValues = new List<(RESTStepParameter parameter, string value)>();

        foreach (var (step, restStepParameter) in AllProperties)
        {
            var r = await step.Run<StringStream>(stateMonad, cancellationToken)
                .Map(x => x.GetStringAsync());

            if (r.IsFailure)
                errors.Add(r.Error);
            else
                parameterValues.Add((restStepParameter, r.Value));
        }

        if (errors.Any())
            return Result.Failure<Entity, IError>(ErrorList.Combine(errors));

        var path = System.IO.Path.Combine(
            Server.Url,
            Path
        );

        IRestRequest request = new RestRequest(path, Method.GET);

        foreach (var (parameter, value) in parameterValues)
        {
            request = request.AddParameter(parameter.Parameter.Name, value);
        }

        var resultString =
            await request.TryRun(stateMonad.RestClient, cancellationToken);

        if (resultString.IsFailure)
            return resultString.ConvertFailure<Entity>().MapError(x => x.WithLocation(this));

        var result = TryDeserializeToEntity(resultString.Value).MapError(x => x.WithLocation(this));

        return result;
    }

    /// <summary>
    /// Try deserialize a string to an entity
    /// </summary>
    public static Result<Entity, IErrorBuilder> TryDeserializeToEntity(string jsonString)
    {
        Entity? entity;

        try
        {
            entity = JsonConvert.DeserializeObject<Entity>(
                jsonString,
                EntityJsonConverter.Instance
            );
        }
        catch (Exception e)
        {
            return ErrorCode.Unknown.ToErrorBuilder(e.Message);
        }

        if (entity is null)
            return ErrorCode.CouldNotParse.ToErrorBuilder(jsonString, "JSON");

        return entity;
    }

    /// <inheritdoc />
    public Result<Unit, IError> Verify(StepFactoryStore stepFactoryStore)
    {
        var r3 = AllProperties
            .Select(x => x.step.Verify(stepFactoryStore));

        var finalResult = r3
            .Combine(ErrorList.Combine)
            .Map(_ => Unit.Default);

        return finalResult;
    }

    /// <inheritdoc />
    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.Append(Name);

        foreach (var (step, parameter) in AllProperties)
        {
            sb.Append(' ');

            sb.Append(parameter.Name);
            sb.Append(": ");

            var value = step.Serialize();

            sb.Append(value);
        }

        return sb.ToString();
    }

    /// <inheritdoc />
    public Task<Result<T1, IError>> Run<T1>(
        IStateMonad stateMonad,
        CancellationToken cancellationToken)
    {
        return Run(stateMonad, cancellationToken)
            .BindCast<Entity, T1, IError>(
                ErrorCode.InvalidCast.ToErrorBuilder(typeof(Entity), typeof(T1)).WithLocation(this)
            );
    }

    /// <inheritdoc />
    public Maybe<EntityValue> TryConvertToEntityValue()
    {
        return Maybe<EntityValue>.None;
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
}

public class RESTStepParameter : IStepParameter
{
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
}

public class RESTGetStepFactory : IStepFactory
{
    public RESTGetStepFactory(
        OpenApiDocument openApiDocument,
        string path,
        OpenApiServer server,
        OpenApiPathItem pathItem,
        OpenApiOperation operation)
    {
        OpenApiDocument = openApiDocument;
        Path            = path;
        Server          = server;
        PathItem        = pathItem;
        Operation       = operation;

        ParameterDictionary =
            Operation.Parameters.OrderByDescending(x => x.Required)
                .Select(x => new RESTStepParameter(x))
                .ToDictionary(
                    x => new StepParameterReference.Named(x.Name)
                        as StepParameterReference,
                    x => x as IStepParameter
                );
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<StepParameterReference, IStepParameter> ParameterDictionary { get; }

    public OpenApiDocument OpenApiDocument { get; }
    public string Path { get; }
    public OpenApiServer Server { get; }
    public OpenApiPathItem PathItem { get; }
    public OpenApiOperation Operation { get; }
    public OperationType OperationType { get; } = OperationType.Get;

    /// <inheritdoc />
    public Result<TypeReference, IError> TryGetOutputTypeReference(
        CallerMetadata callerMetadata,
        FreezableStepData freezeData,
        TypeResolver typeResolver)
    {
        return TypeReference.Actual.Entity;
    }

    /// <inheritdoc />
    public IEnumerable<UsedVariable> GetVariablesUsed(
        CallerMetadata callerMetadata,
        FreezableStepData freezableStepData,
        TypeResolver typeResolver)
    {
        yield break;
    }

    /// <inheritdoc />
    public Result<IStep, IError> TryFreeze(
        CallerMetadata callerMetadata,
        TypeResolver typeResolver,
        FreezableStepData freezeData)
    {
        if (!callerMetadata.ExpectedType.Allow(TypeReference.Actual.Entity, typeResolver))
            return Result.Failure<IStep, IError>(
                ErrorCode.WrongType.ToErrorBuilder(
                        callerMetadata.ExpectedType,
                        callerMetadata.ParameterName,
                        TypeName,
                        nameof(Entity)
                    )
                    .WithLocation(freezeData.Location)
            );

        var allProperties = new List<(IStep step, RESTStepParameter restStepParameter)>();
        var errors        = new List<IError>();

        foreach (var (stepParameterReference, stepParameter) in ParameterDictionary)
        {
            if (freezeData.StepProperties.TryGetValue(
                stepParameterReference,
                out var value
            ))
            {
                var nestedCallerMetadata = new CallerMetadata(
                    TypeName,
                    stepParameter.Name,
                    TypeReference.Create(stepParameter.ActualType)
                );

                var frozenStep =
                    value.ConvertToStep().TryFreeze(nestedCallerMetadata, typeResolver);

                if (frozenStep.IsFailure)
                    errors.Add(frozenStep.Error);

                allProperties.Add(new(frozenStep.Value, (RESTStepParameter)stepParameter));
            }
            else if (stepParameter.Required)
            {
                errors.Add(
                    ErrorCode.MissingParameter.ToErrorBuilder(stepParameter.Name)
                        .WithLocation(freezeData.Location)
                );
            }
        }

        if (errors.Any())
        {
            return Result.Failure<IStep, IError>(ErrorList.Combine(errors));
        }

        return new RESTDynamicGet(
            OpenApiDocument,
            Path,
            Server,
            PathItem,
            Operation,
            allProperties,
            freezeData.Location
        );
    }

    /// <inheritdoc />
    public string TypeName =>
        Helpers.CreateStepName(OpenApiDocument.Info.Title, Path, OperationType);

    /// <inheritdoc />
    public string Category => "REST";

    /// <inheritdoc />
    public IStepSerializer Serializer => new FunctionSerializer(TypeName);

    /// <inheritdoc />
    public IEnumerable<Requirement> Requirements
    {
        get
        {
            yield break;
        }
    }

    /// <inheritdoc />
    public string OutputTypeExplanation => "Entity";

    /// <inheritdoc />
    public IEnumerable<Type> EnumTypes
    {
        get
        {
            yield break;
        }
    }

    /// <inheritdoc />
    public string Summary => Operation.Summary;

    /// <inheritdoc />
    public IEnumerable<string> Names
    {
        get
        {
            yield return TypeName;
        }
    }

    /// <inheritdoc />
    public IEnumerable<SCLExample> Examples
    {
        get
        {
            yield break;
        }
    }
}

}
