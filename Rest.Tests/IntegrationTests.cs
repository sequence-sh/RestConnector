using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Reductech.Sequence.ConnectorManagement.Base;
using Reductech.Sequence.Core.Abstractions;
using Reductech.Sequence.Core.Internal.Serialization;
using Xunit;
using Xunit.Abstractions;

namespace Reductech.Sequence.Connectors.Rest.Tests;

[AutoTheory.UseTestOutputHelper]
public partial class IntegrationTests
{
    public const string Skip = "skip";

    [Fact(Skip = Skip)]
    public async Task TestGetStream()
    {
        var scl = "log (RestGetStream 'https://en.wikipedia.org/' 'wiki/Tokyo')";
        var r   = await RunSCL(scl, null, TestOutputHelper);

        r.ShouldBeSuccessful();
    }

    private static async Task<Result<Unit, IError>> RunSCL(
        string scl,
        OpenAPISpecification? specification,
        ITestOutputHelper testOutputHelper)
    {
        var dictionary =
            specification is null
                ? new Dictionary<string, object>()
                : new Dictionary<string, object>()
                {
                    {
                        DynamicStepGenerator.SpecificationsKey,
                        EntityConversionHelpers.ConvertToEntity(specification)
                    }
                };

        var externalContext = ExternalContext.Default;

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

        var runner = new SCLRunner(
            new TestOutputLogger("Test", testOutputHelper),
            stepFactoryResult.Value,
            externalContext
        );

        var result = await runner.RunSequenceFromTextAsync(
            scl,
            new Dictionary<string, object>(),
            CancellationToken.None
        );

        return result;
    }

    [Fact(Skip = Skip)]
    public async Task TestRevealPOSTSequence()
    {
        var specificationText = SpecificationExamples.RevealJson;

        var spec = new OpenAPISpecification(
            "Reveal",
            "https://salient-eu.revealdata.com/rest",
            specificationText,
            null,
            null,
            null
        );

        var scl =
            @"- <caseId> = '159'
- <folderBody> = (""Name"": ""MyTestFolder"")


- <session> = Reveal_Login_Get username: 'username' password: 'password'
- <userId> = <session>[""UserId""]
- <LoginSessionId> = <session>[""LoginSessionId""]

- <folderCreateResult> = (Reveal_WorkFolder_Create Body: <folderBody> caseId: <caseId> InControlAuthToken: <LoginSessionId> userId: <userId>)";

        var result = await RunSCL(scl, spec, TestOutputHelper);

        result.ShouldBeSuccessful();
    }

    [Fact(Skip = Skip)]
    public void TestGeneratingFromFile()
    {
        var dictionary = new Dictionary<string, object>()
        {
            {
                DynamicStepGenerator.SpecificationsKey, EntityConversionHelpers.ConvertToEntity(
                    new OpenAPISpecification(
                        "Reveal",
                        "http://test.com",
                        null,
                        null,
                        @"C:\Users\wainw\source\repos\Reductech\rest\Rest.Tests\Resources\RevealJson.txt",
                        null
                    )
                )
            }
        };

        var assembly = Assembly.GetAssembly(typeof(RESTDynamicStep<>));

        var context = new ExternalContext(
            ExternalContext.Default.ExternalProcessRunner,
            ExternalContext.Default.RestClientFactory,
            ExternalContext.Default.Console,
            (ConnectorInjection.FileSystemKey, new FileSystem())
        );

        var result =
            StepFactoryStore.TryCreate(
                context,
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

        result.ShouldBeSuccessful();
        result.Value.Dictionary.Keys.Should().Contain("Reveal_Cases_Get");
    }

    [Fact(Skip = Skip)]
    public void TestGeneratingFromURL()
    {
        var dictionary = new Dictionary<string, object>()
        {
            {
                DynamicStepGenerator.SpecificationsKey, EntityConversionHelpers.ConvertToEntity(
                    new OpenAPISpecification(
                        "Reveal",
                        "http://test.com",
                        null,
                        "https://salient-eu.revealdata.com/rest/swagger/docs/V2",
                        null,
                        null
                    )
                )
            }
        };

        var assembly = Assembly.GetAssembly(typeof(RESTDynamicStep<>));

        var result =
            StepFactoryStore.TryCreate(
                ExternalContext.Default,
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

        result.ShouldBeSuccessful();
        result.Value.Dictionary.Keys.Should().Contain("Reveal_Cases_Get");
    }
}
