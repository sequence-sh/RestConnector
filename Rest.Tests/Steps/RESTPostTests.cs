using System.Net;

namespace Sequence.Connectors.Rest.Tests.Steps;

public partial class RESTPostTests : StepTestBase<RESTPost, StringStream>
{
    /// <inheritdoc />
    protected override IEnumerable<StepCase> StepCases
    {
        get
        {
            yield return new StepCase(
                    "Basic Case",
                    new RESTPost()
                    {
                        BaseURL     = Constant("http://www.abc.com"),
                        RelativeURL = Constant("Thing/1"),
                        Entity      = Constant(Entity.Create(("a", 123)))
                    },
                    new StringStream("12345")
                )
                .SetupHTTPSuccess(
                    "http://www.abc.com",
                    ("Thing/1", Method.Post, "{\"a\":123}"),
                    true,
                    HttpStatusCode.OK,
                    "12345"
                );
        }
    }
}
