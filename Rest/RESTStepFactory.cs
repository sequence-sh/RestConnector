using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using CSharpFunctionalExtensions;
using Microsoft.OpenApi.Models;
using Reductech.EDR.Connectors.Rest.Errors;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Attributes;
using Reductech.EDR.Core.Entities;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.Util;
using Entity = Reductech.EDR.Core.Entity;

namespace Reductech.EDR.Connectors.Rest
{

/// <inheritdoc />
public class RESTStepFactory : IStepFactory
{
    /// <summary>
    /// Create a new RESTStepFactory
    /// </summary>
    /// <param name="operationMetadata"></param>
    public RESTStepFactory(OperationMetadata operationMetadata)
    {
        OperationMetadata = operationMetadata;

        var securityParameters =
            operationMetadata.Operation.Security.SelectMany(x => x)
                .Select(x => new RESTStepSecurityParameter(x.Key));

        ParameterDictionary =
            OperationMetadata.Operation.Parameters.OrderByDescending(x => x.Required)
                .Select(x => new RESTStepParameter(x) as IRESTStepParameter)
                .Concat(securityParameters)
                .GroupBy(x => x.Name)
                .ToDictionary(
                    x => new StepParameterReference.Named(x.Key)
                        as StepParameterReference,
                    x => x.First() as IStepParameter
                );
    }

    /// <inheritdoc />
    public IReadOnlyDictionary<StepParameterReference, IStepParameter> ParameterDictionary { get; }

    /// <summary>
    /// The Operation Metadata
    /// </summary>
    public OperationMetadata OperationMetadata { get; }

    /// <inheritdoc />
    public Result<TypeReference, IError> TryGetOutputTypeReference(
        CallerMetadata callerMetadata,
        FreezableStepData freezeData,
        TypeResolver typeResolver)
    {
        var r1 = GetTypeReference(OperationMetadata.OperationType);

        if (r1 is not null)
            return r1;

        return Result.Failure<TypeReference, IError>(
            ErrorCodeREST.OperationNotImplemented.ToErrorBuilder(OperationMetadata.OperationType)
                .WithLocation(freezeData)
        );
    }

    /// <summary>
    /// Gets the output type reference from an operation type
    /// </summary>
    public static TypeReference? GetTypeReference(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Get    => TypeReference.Actual.Entity,
            OperationType.Put    => TypeReference.Unit.Instance,
            OperationType.Post   => TypeReference.Actual.String,
            OperationType.Delete => TypeReference.Unit.Instance,
            OperationType.Patch  => TypeReference.Unit.Instance,
            _                    => null
        };
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

        var allProperties = new List<(IStep step, IRESTStepParameter restStepParameter)>();
        var errors        = new List<IError>();

        foreach (var (stepParameterReference, sp1) in ParameterDictionary)
        {
            var stepParameter = (RESTStepParameter)sp1;

            if (freezeData.StepProperties.TryGetValue(
                stepParameterReference,
                out var value
            ))
            {
                var nestedCallerMetadata = new CallerMetadata(
                    TypeName,
                    stepParameter.Name,
                    TypeReference.Create(sp1.ActualType)
                );

                var frozenStep =
                    value.ConvertToStep().TryFreeze(nestedCallerMetadata, typeResolver);

                if (frozenStep.IsFailure)
                    errors.Add(frozenStep.Error);

                allProperties.Add(new(frozenStep.Value, stepParameter));
            }
            else if (stepParameter.Required)
            {
                var defaultValue = stepParameter.Parameter.Schema.Default;

                if (defaultValue is not null)
                {
                    var constantString = new StringConstant(defaultValue.ToString()!);
                    allProperties.Add(new(constantString, stepParameter));
                }
                else
                {
                    errors.Add(
                        ErrorCode.MissingParameter.ToErrorBuilder(stepParameter.Name)
                            .WithLocation(freezeData.Location)
                    );
                }
            }
        }

        if (errors.Any())
        {
            return Result.Failure<IStep, IError>(ErrorList.Combine(errors));
        }

        var typeReference = GetTypeReference(OperationMetadata.OperationType);

        if (typeReference == TypeReference.Actual.Entity)
        {
            return new RESTDynamicStep<Entity>(
                OperationMetadata,
                TryDeserializeToEntity,
                allProperties,
                freezeData.Location
            );
        }

        if (typeReference == TypeReference.Unit.Instance)
        {
            return new RESTDynamicStep<Unit>(
                OperationMetadata,
                _ => Unit.Default,
                allProperties,
                freezeData.Location
            );
        }

        if (typeReference == TypeReference.Actual.String)
        {
            return new RESTDynamicStep<StringStream>(
                OperationMetadata,
                s => new StringStream(s),
                allProperties,
                freezeData.Location
            );
        }

        return Result.Failure<IStep, IError>(
            ErrorCodeREST.OperationNotImplemented.ToErrorBuilder(OperationMetadata.OperationType)
                .WithLocation(freezeData)
        );
    }

    /// <inheritdoc />
    public string TypeName => OperationMetadata.Name;

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
    public string Summary => OperationMetadata.Operation.Summary;

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

    /// <summary>
    /// Try deserialize a string to an entity
    /// </summary>
    public static Result<Entity, IErrorBuilder> TryDeserializeToEntity(string jsonString)
    {
        Entity? entity;

        try
        {
            var options = new JsonSerializerOptions()
            {
                Converters = { new JsonStringEnumConverter(), VersionJsonConverter.Instance },
                PropertyNameCaseInsensitive = true
            };

            entity = JsonSerializer.Deserialize<Entity>(
                jsonString,
                options
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
}

}
