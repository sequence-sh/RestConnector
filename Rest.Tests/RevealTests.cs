using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Moq;
using Reductech.Sequence.ConnectorManagement.Base;
using Reductech.Sequence.Core.Abstractions;
using Reductech.Sequence.Core.ExternalProcesses;
using Reductech.Sequence.Core.Internal.Serialization;
using Reductech.Sequence.Core.TestHarness.Rest;
using RestSharp;
using Xunit;

namespace Reductech.Sequence.Connectors.Rest.Tests;

[AutoTheory.UseTestOutputHelper]
public partial class RevealTests
{
    [Fact]
    public async Task TestRevealPOSTSequence()
    {
        var specificationText = SpecificationExamples.RevealJson;

        var dictionary = new Dictionary<string, object>()
        {
            {
                DynamicStepGenerator.SpecificationsKey, EntityConversionHelpers.ConvertToEntity(
                    new OpenAPISpecification(
                        "Reveal",
                        "http://test.com",
                        specificationText,
                        null,
                        null,
                        null
                    )
                )
            }
        };

        var mockRepo = new MockRepository(MockBehavior.Strict);

        var setupHelper = new RESTClientSetupHelper();

        setupHelper.GetRESTClientFactory(mockRepo);

        setupHelper.AddHttpTestAction(
            new RESTSetup(
                "http://test.com",
                x => CheckPostRequest(x),
                new RestResponse()
                {
                    Content             = "{\"a\": 1}",
                    ResponseStatus      = ResponseStatus.Completed,
                    StatusCode          = HttpStatusCode.Created,
                    IsSuccessStatusCode = true
                }
            )
        );

        var externalContext = new ExternalContext(
            mockRepo.OneOf<IExternalProcessRunner>(),
            setupHelper.GetRESTClientFactory(mockRepo),
            mockRepo.OneOf<IConsole>()
        );

        var assembly = Assembly.GetAssembly(typeof(RESTDynamicStep<>));

        var stepFactoryResult =
            StepFactoryStore.TryCreate(
                externalContext,
                new ConnectorData(
                    new ConnectorSettings()
                    {
                        Enable   = true,
                        Id       = "Reductech.Sequence.Connectors.Rest",
                        Settings = dictionary,
                        Version  = "1.0"
                    },
                    assembly
                )
            );

        stepFactoryResult.ShouldBeSuccessful();
        stepFactoryResult.Value.Dictionary.Keys.Should().Contain("Reveal_Cases_Get");

        var runner = new SCLRunner(
            new TestOutputLogger("Test", TestOutputHelper),
            stepFactoryResult.Value,
            externalContext
        );

        var result = await runner.RunSequenceFromTextAsync(
            "- AssertEqual ('a': 1) (Reveal_WorkFolder_Create caseId: 159 InControlAuthToken: '456' userId: 789 body: (name: 'myNewFolder'))",
            new Dictionary<string, object>(),
            CancellationToken.None
        );

        result.ShouldBeSuccessful();
    }

    static bool CheckPostRequest(RestRequest restRequest)
    {
        return restRequest.Method == Method.Post && restRequest.Parameters.Count == 4;
    }

    [Fact]
    public async Task TestRevealGETSequence()
    {
        var specificationText = SpecificationExamples.RevealJson;

        var dictionary = new Dictionary<string, object>()
        {
            {
                DynamicStepGenerator.SpecificationsKey, EntityConversionHelpers.ConvertToEntity(
                    new OpenAPISpecification(
                        "Reveal",
                        "http://test.com",
                        specificationText,
                        null,
                        null,
                        null
                    )
                )
            }
        };

        var mockRepo = new MockRepository(MockBehavior.Strict);

        var setupHelper = new RESTClientSetupHelper();

        setupHelper.AddHttpTestAction(
            new RESTSetup(
                "http://test.com",
                rc => rc.Method == Method.Get && rc.Parameters.Count == 1,
                new RestResponse()
                {
                    Content             = "{\"a\": 1}",
                    ResponseStatus      = ResponseStatus.Completed,
                    StatusCode          = HttpStatusCode.Created,
                    IsSuccessStatusCode = true
                }
            )
        );

        var externalContext = new ExternalContext(
            mockRepo.OneOf<IExternalProcessRunner>(),
            setupHelper.GetRESTClientFactory(mockRepo),
            mockRepo.OneOf<IConsole>()
        );

        var assembly = Assembly.GetAssembly(typeof(RESTDynamicStep<>));

        var stepFactoryResult =
            StepFactoryStore.TryCreate(
                externalContext,
                new ConnectorData(
                    new ConnectorSettings()
                    {
                        Enable   = true,
                        Id       = "Reductech.Sequence.Connectors.Rest",
                        Settings = dictionary,
                        Version  = "1.0"
                    },
                    assembly
                )
            );

        stepFactoryResult.ShouldBeSuccessful();
        stepFactoryResult.Value.Dictionary.Keys.Should().Contain("Reveal_Cases_Get");

        var runner = new SCLRunner(
            new TestOutputLogger("Test", TestOutputHelper),
            stepFactoryResult.Value,
            externalContext
        );

        var result = await runner.RunSequenceFromTextAsync(
            "- AssertEqual ('a': 1) (Reveal_Cases_Get InControlAuthToken: '456')",
            new Dictionary<string, object>(),
            CancellationToken.None
        );

        result.ShouldBeSuccessful();
    }
}
