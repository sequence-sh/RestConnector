using System;
using System.Collections.Generic;
using System.IO.Abstractions;
using System.Linq;
using CSharpFunctionalExtensions;
using Microsoft.OpenApi.Readers;
using Reductech.EDR.Connectors.Rest.Errors;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Errors;
using RestSharp;

namespace Reductech.EDR.Connectors.Rest
{

/// <summary>
/// An OpenAPI specification in the settings file
/// </summary>
public record OpenAPISpecification(
    string Name,
    string BaseURL,
    string? Specification,
    string? SpecificationURL,
    string? SpecificationFilePath)
{
    /// <summary>
    /// Tries to get the Specification from the settings object
    /// </summary>
    public Result<IReadOnlyList<IStepFactory>, IErrorBuilder> TryGetStepFactories(
        IExternalContext externalContext)
    {
        var setFields = 0;

        if (!string.IsNullOrWhiteSpace(Specification))
            setFields++;

        if (!string.IsNullOrWhiteSpace(SpecificationURL))
            setFields++;

        if (!string.IsNullOrWhiteSpace(SpecificationFilePath))
            setFields++;

        if (setFields != 1)
        {
            return ErrorCodeREST.InvalidSpecification.ToErrorBuilder(
                $"Exactly one of {nameof(Specification)}, {nameof(SpecificationURL)} and {SpecificationFilePath} must be set."
            );
        }

        string specificationText;

        if (!string.IsNullOrWhiteSpace(Specification))
        {
            specificationText = Specification;
        }
        else if (!string.IsNullOrWhiteSpace(SpecificationURL))
        {
            var restClient = externalContext.RestClientFactory.CreateRestClient("");
            var request    = new RestRequest(SpecificationURL, Method.GET);

            var response = restClient.Execute(request);

            if (response.IsSuccessful)
                specificationText = response.Content;
            else
                return ErrorCodeREST.CouldNotLoadSpecification.ToErrorBuilder(
                    response.ErrorMessage
                );
        }
        else
        {
            var fileSystemResult =
                externalContext.TryGetContext<IFileSystem>(ConnectorInjection.FileSystemKey);

            if (fileSystemResult.IsFailure)
                return fileSystemResult.ConvertFailure<IReadOnlyList<IStepFactory>>();

            try
            {
                specificationText = fileSystemResult.Value.File.ReadAllText(SpecificationFilePath);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        return CreateStepFactories(Name, BaseURL, specificationText);
    }

    /// <summary>
    /// Create Step Factories from an OpenAPI specification
    /// </summary>
    public static Result<IReadOnlyList<IStepFactory>, IErrorBuilder> CreateStepFactories(
        string specificationName,
        string specificationBaseUrl,
        string specificationText)
    {
        var stream = new StringStream(specificationText);

        var openApiDocument = new OpenApiStreamReader().Read(
            stream.GetStream().stream,
            out var diagnostic
        );

        if (openApiDocument is null)
        {
            var errorBuilder = ErrorBuilderList.Combine(
                diagnostic.Errors.Select(
                    x => ErrorCodeREST.InvalidSpecification.ToErrorBuilder(
                        $"{x.Message} as {x.Pointer}"
                    )
                )
            );

            return Result.Failure<IReadOnlyList<IStepFactory>, IErrorBuilder>(errorBuilder);
        }

        if (openApiDocument.Paths is null)
            return Result.Failure<IReadOnlyList<IStepFactory>, IErrorBuilder>(
                ErrorCodeREST.InvalidSpecification.ToErrorBuilder(
                    $"{nameof(openApiDocument.Paths)} is not set"
                )
            );

        var factories = new List<IStepFactory>();

        foreach (var (path, pathItem) in openApiDocument.Paths)
        foreach (var (operationType, openApiOperation) in pathItem.Operations)
        {
            var metadata = new OperationMetadata(
                specificationName,
                openApiDocument,
                path,
                specificationBaseUrl,
                pathItem,
                openApiOperation,
                operationType
            );

            factories.Add(new RESTStepFactory(metadata));
        }

        return factories;
    }
}

}
