﻿using Reductech.Sequence.Core.Internal.Errors;
using RestSharp;

namespace Reductech.Sequence.Connectors.Rest;

/// <summary>
/// Contains methods to help with Flurl Requests
/// </summary>
public static class RestHelpers
{
    /// <summary>
    /// Add entity properties as headers to this request
    /// </summary>
    public static RestRequest AddHeaders(this RestRequest request, Entity entity)
    {
        foreach (var (name, sclObject) in entity)
        {
            request = request.AddHeader(
                name.Inner,
                sclObject.Serialize(SerializeOptions.Primitive)
            );
        }

        return request;
    }

    /// <summary>
    /// Execute this request. Returns an error if it fails
    /// </summary>
    public static async Task<Result<string, IErrorBuilder>> TryRun(
        this RestRequest request,
        IRestClient client,
        CancellationToken cancellationToken)
    {
        RestResponse response;

        try
        {
            response = await client.ExecuteAsync(request, cancellationToken);
        }
        catch (Exception ex)
        {
            return ErrorCode.Unknown.ToErrorBuilder(ex);
        }

        //response.

        if (response.IsSuccessful)
            return response.Content ?? "";

        return ErrorCode.RequestFailed
            .ToErrorBuilder(
                response.StatusCode,
                response.StatusDescription ?? "",
                response.ErrorMessage ?? ""
            );
    }
}
