using System.Collections.Generic;
using System.IO.Abstractions;
using System.Reflection;
using FluentAssertions;
using Reductech.EDR.ConnectorManagement.Base;
using Reductech.EDR.Core;
using Reductech.EDR.Core.Abstractions;
using Reductech.EDR.Core.Internal;
using Reductech.EDR.Core.TestHarness;
using Xunit;

namespace Reductech.EDR.Connectors.Rest.Tests
{

[AutoTheory.UseTestOutputHelper]
public partial class IntegrationTests
{
    public const string Skip = "skip";

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
