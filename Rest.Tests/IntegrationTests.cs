using System.Collections.Generic;
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
    [Fact(Skip = "Manual")]
    public void TestGenerating()
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
