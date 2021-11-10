﻿using System;
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
    public RESTStepFactory(OperationMetadata operationMetadata)
    {
        OperationMetadata = operationMetadata;

        var securityParameters = OperationMetadata.Operation.Security.SelectMany(x => x.Keys)
            .Select(x => new RESTStepSecurityParameter(x));

        var bodyParameters = operationMetadata.Operation.RequestBody is null
            ? new List<IStepParameter>()
            : new List<IStepParameter>()
            {
                new RESTStepBodyParameter(operationMetadata.Operation.RequestBody)
            };

        ParameterDictionary =
            OperationMetadata.Operation.Parameters.OrderByDescending(x => x.Required)
                .Select(x => new RESTStepParameter(x) as IStepParameter)
                .Concat(securityParameters)
                .Concat(bodyParameters)
                .GroupBy(x => x.Name)
                .ToDictionary(
                    x => new StepParameterReference.Named(x.Key)
                        as StepParameterReference,
                    x => x.First()
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
            OperationType.Post   => TypeReference.Actual.Entity,
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
        var typeReference = GetTypeReference(OperationMetadata.OperationType)
                         ?? TypeReference.Actual.Entity;

        if (!callerMetadata.ExpectedType.Allow(typeReference, typeResolver))
            return Result.Failure<IStep, IError>(
                ErrorCode.WrongType.ToErrorBuilder(
                        callerMetadata.StepName,
                        callerMetadata.ExpectedType.Name,
                        callerMetadata.ParameterName,
                        TypeName,
                        nameof(Entity)
                    )
                    .WithLocation(freezeData.Location)
            );

        var allProperties = new List<(IStep step, IRESTStepParameter restStepParameter)>();

        Maybe<(IStep<Entity>?, RESTStepBodyParameter)> bodyParameter =
            Maybe<(IStep<Entity>?, RESTStepBodyParameter)>.None;

        var errors = new List<IError>();

        foreach (var (stepParameterReference, sp1) in ParameterDictionary)
        {
            if (sp1 is IRESTStepParameter stepParameter)
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

                    allProperties.Add(new(frozenStep.Value, stepParameter));
                }
                else if (stepParameter.Required)
                {
                    var defaultValue = stepParameter.DefaultValue?.ToString();

                    if (defaultValue is not null)
                    {
                        var constantString = new StringConstant(defaultValue);
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
            else if (sp1 is RESTStepBodyParameter bodyParameter1)
            {
                if (freezeData.StepProperties.TryGetValue(
                        stepParameterReference,
                        out var value
                    ))
                {
                    var nestedCallerMetadata = new CallerMetadata(
                        TypeName,
                        bodyParameter1.Name,
                        TypeReference.Actual.Entity
                    );

                    var frozenStep =
                        value.ConvertToStep().TryFreeze(nestedCallerMetadata, typeResolver);

                    if (frozenStep.IsFailure)
                        errors.Add(frozenStep.Error);

                    if (frozenStep.Value is IStep<Entity> entityStep)
                    {
                        bodyParameter = Maybe<(IStep<Entity>?, RESTStepBodyParameter)>.From(
                            (entityStep, bodyParameter1)
                        );
                    }
                    else
                    {
                        errors.Add(
                            ErrorCode.MissingParameter.ToErrorBuilder(bodyParameter1.Name)
                                .WithLocation(freezeData.Location)
                        );
                    }
                }
                else if (bodyParameter1.Required)
                {
                    errors.Add(
                        ErrorCode.MissingParameter.ToErrorBuilder(bodyParameter1.Name)
                            .WithLocation(freezeData.Location)
                    );
                }
            }
        }

        if (errors.Any())
        {
            return Result.Failure<IStep, IError>(ErrorList.Combine(errors));
        }

        if (typeReference == TypeReference.Actual.Entity)
        {
            return new RESTDynamicStep<Entity>(
                OperationMetadata,
                TryDeserializeToEntity,
                allProperties,
                bodyParameter,
                freezeData.Location
            );
        }

        if (typeReference == TypeReference.Unit.Instance)
        {
            return new RESTDynamicStep<Unit>(
                OperationMetadata,
                _ => Unit.Default,
                allProperties,
                bodyParameter,
                freezeData.Location
            );
        }

        if (typeReference == TypeReference.Actual.String)
        {
            return new RESTDynamicStep<StringStream>(
                OperationMetadata,
                s => new StringStream(s),
                allProperties,
                bodyParameter,
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
