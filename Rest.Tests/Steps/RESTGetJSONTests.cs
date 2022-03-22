using System.Net;
using Reductech.Sequence.Connectors.Rest.Steps;
using RestSharp;

namespace Reductech.Sequence.Connectors.Rest.Tests.Steps;

public partial class RESTGetJSONTests : StepTestBase<RESTGetJSON, Entity>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                    "Basic Case",
                    new RESTGetJSON()
                    {
                        BaseURL     = Constant("http://www.abc.com"),
                        RelativeURL = Constant("Thing/1")
                    },
                    Entity.Create(("a", 1))
                )
                .SetupHTTPSuccess(
                    "http://www.abc.com",
                    ("Thing/1", Method.Get, null),
                    true,
                    HttpStatusCode.OK,
                    "{\"a\": 1}"
                );
        }
    }
}
