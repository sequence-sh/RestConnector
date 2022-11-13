﻿namespace Sequence.Connectors.Rest.Tests.Steps;

public partial class RESTDeleteTests : StepTestBase<RESTDelete, Unit>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                    "Basic Case",
                    new RESTDelete()
                    {
                        BaseURL     = Constant("http://www.abc.com"),
                        RelativeURL = Constant("Thing/1")
                    },
                    Unit.Default
                )
                .SetupHTTPSuccess(
                    "http://www.abc.com",
                    ("Thing/1", Method.Delete, null),
                    true,
                    HttpStatusCode.OK
                );
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<ErrorCase> ErrorCases
    {
        get
        {
            yield return new ErrorCase(
                "Request Failure",
                new RESTDelete()
                {
                    BaseURL = Constant("http://www.abc.com"), RelativeURL = Constant("Thing/1")
                },
                ErrorCode.RequestFailed.ToErrorBuilder(
                    HttpStatusCode.Forbidden,
                    "Test Forbidden",
                    "Test Error"
                )
            ).SetupHTTPError(
                "http://www.abc.com",
                ("Thing/1", Method.Delete, null),
                HttpStatusCode.Forbidden,
                "Test Forbidden",
                "Test Error"
            );

            foreach (var errorCase in base.ErrorCases)
            {
                yield return errorCase;
            }
        }
    }
}
