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
using RestSharp;
using RestSharp.Serialization;
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

        var restClientMock = mockRepo.Create<IRestClient>();

        var externalContext = new ExternalContext(
            mockRepo.OneOf<IExternalProcessRunner>(),
            new SingleRestClientFactory(restClientMock.Object),
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

        var uri = new Uri("http://test.com/");
        restClientMock.SetupSet(x => x.BaseUrl = uri);

        restClientMock.Setup(x => x.UseSerializer(It.IsAny<Func<IRestSerializer>>()))
            .Returns(restClientMock.Object);

        restClientMock.Setup(
                rc => rc.ExecuteAsync(
                    It.Is<IRestRequest>((rr) => CheckPostRequest(rr)),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new RestResponse()
                {
                    Content        = "{\"a\": 1}",
                    ResponseStatus = ResponseStatus.Completed,
                    StatusCode     = HttpStatusCode.Created
                }
            );

        var result = await runner.RunSequenceFromTextAsync(
            "- AssertEqual ('a': 1) (Reveal_WorkFolder_Create caseId: '159' InControlAuthToken: '456' userId: '789' body: (name: 'myNewFolder'))",
            new Dictionary<string, object>(),
            CancellationToken.None
        );

        result.ShouldBeSuccessful();
    }

    static bool CheckPostRequest(IRestRequest restRequest)
    {
        return restRequest.Method == Method.POST && restRequest.Parameters.Count == 4;
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

        var restClientMock = mockRepo.Create<IRestClient>();

        var externalContext = new ExternalContext(
            mockRepo.OneOf<IExternalProcessRunner>(),
            new SingleRestClientFactory(restClientMock.Object),
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

        var uri = new Uri("http://test.com/");
        restClientMock.SetupSet(x => x.BaseUrl = uri);

        restClientMock.Setup(x => x.UseSerializer(It.IsAny<Func<IRestSerializer>>()))
            .Returns(restClientMock.Object);

        restClientMock.Setup(
                rc => rc.ExecuteAsync(
                    It.Is<IRestRequest>(
                        rr =>
                            rr.Method == Method.GET && rr.Parameters.Count == 1
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new RestResponse()
                {
                    Content        = "{\"a\": 1}",
                    ResponseStatus = ResponseStatus.Completed,
                    StatusCode     = HttpStatusCode.Created
                }
            );

        var result = await runner.RunSequenceFromTextAsync(
            "- AssertEqual ('a': 1) (Reveal_Cases_Get InControlAuthToken: '456')",
            new Dictionary<string, object>(),
            CancellationToken.None
        );

        result.ShouldBeSuccessful();
    }
}
