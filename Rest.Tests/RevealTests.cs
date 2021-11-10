using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Moq;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.ExternalProcesses;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.TestHarness;
using RestSharp;
using Xunit;

namespace Reductech.EDR.Connectors.Rest.Tests
{

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
                        Id       = "Reductech.EDR.Connectors.Rest",
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

        restClientMock.Setup(
                rc => rc.ExecuteAsync(
                    It.Is<IRestRequest>(
                        rr =>
                            rr.Method == Method.POST && rr.Parameters.Count == 3
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
            "- AssertEqual ('a': 1) (Reveal_WorkFolder_Create caseId: '159' InControlAuthToken: '456' userId: '789')",
            new Dictionary<string, object>(),
            CancellationToken.None
        );

        result.ShouldBeSuccessful();
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
                        Id       = "Reductech.EDR.Connectors.Rest",
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

}
