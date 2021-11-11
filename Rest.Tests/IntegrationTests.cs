using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Divergic.Logging.Xunit;
using FluentAssertions;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.Internal.Serialization;
using Reductech.EDR.Core.TestHarness;
using Xunit;

namespace Reductech.EDR.Connectors.Rest.Tests
{

[AutoTheory.UseTestOutputHelper]
public partial class IntegrationTests
{
    public const string Skip = "skip";

    [Fact(Skip = Skip)]
    public async Task TestRevealPOSTSequence()
    {
        var specificationText = SpecificationExamples.RevealJson;

        var dictionary = new Dictionary<string, object>()
        {
            {
                DynamicStepGenerator.SpecificationsKey, EntityConversionHelpers.ConvertToEntity(
                    new OpenAPISpecification(
                        "Reveal",
                        "https://salient-eu.revealdata.com/rest",
                        specificationText,
                        null,
                        null
                    )
                )
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

        var scl =
            @"- <caseId> = '159'
- <folderBody> = (""Name"": ""MyTestFolder"")


- <session> = Reveal_Login_Get username: 'username' password: 'password'
- <userId> = <session>[""UserId""]
- <LoginSessionId> = <session>[""LoginSessionId""]

- <folderCreateResult> = (Reveal_WorkFolder_Create Body: <folderBody> caseId: <caseId> InControlAuthToken: <LoginSessionId> userId: <userId>)";

        var result = await runner.RunSequenceFromTextAsync(
            scl,
            new Dictionary<string, object>(),
            CancellationToken.None
        );

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
                        @"C:\Users\wainw\source\repos\Reductech\rest\Rest.Tests\Resources\RevealJson.txt"
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
                        Id       = "Reductech.EDR.Connectors.Rest",
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
                        Id       = "Reductech.EDR.Connectors.Rest",
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

}
