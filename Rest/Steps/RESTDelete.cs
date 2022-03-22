using Reductech.Sequence.Core.Internal.Errors;
using RestSharp;

namespace Reductech.Sequence.Connectors.Rest.Steps;

/// <summary>
/// Delete a REST resource
/// </summary>
public sealed class RESTDelete : RESTStep<Unit>
{
    /// <inheritdoc />
    public override IStepFactory StepFactory { get; } = new SimpleStepFactory<RESTDelete, Unit>();

    /// <inheritdoc />
    public override Method Method => Method.Delete;

    /// <inheritdoc />
    protected override async Task<Result<RestRequest, IError>> SetRequestBody(
        IStateMonad stateMonad,
        RestRequest restRequest,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return Result.Success<RestRequest, IError>(restRequest);
    }

    /// <inheritdoc />
    protected override Result<Unit, IErrorBuilder> GetResult(string s)
    {
        return Unit.Default;
    }
}
