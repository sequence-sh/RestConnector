using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Reductech.EDR.Connectors.Rest.Tests
{

public class StepGenerationTests
{
    [Theory]
    [InlineData(nameof(SpecificationExamples.Example), "Example_users_Get")]
    [InlineData(
        nameof(SpecificationExamples.Orchestrator),
        "Orchestrator_Bags_Get;Orchestrator_Bags_Post;Orchestrator_api_v_version_Bags_id_Get;Orchestrator_api_v_version_Bags_id_Patch;Orchestrator_api_v_version_Bags_id_Put;Orchestrator_api_v_version_Bags_id_Delete;Orchestrator_Bag_Sequences;Orchestrator_Logs_Get;Orchestrator_Runs_Get;Orchestrator_Runs_Post;Orchestrator_api_v_version_Runs_id_Get;Orchestrator_api_v_version_Runs_id_Patch;Orchestrator_api_v_version_Runs_id_Put;Orchestrator_api_v_version_Runs_id_Delete;Orchestrator_Sequence_Runs;Orchestrator_Sequences_Get;Orchestrator_Sequences_Post;Orchestrator_api_v_version_Sequences_id_Get;Orchestrator_api_v_version_Sequences_id_Patch;Orchestrator_api_v_version_Sequences_id_Put;Orchestrator_api_v_version_Sequences_id_Delete"
    )]
    public void TestFactoryNames(string specificationName, string expectedNamesString)
    {
        var specificationText = SpecificationExamples.ResourceManager.GetString(specificationName)!;

        var specification = new OpenAPISpecification(
            specificationName,
            "http://baseURL",
            specificationText
        );

        var factories = DynamicStepGenerator
            .CreateStepFactories(specification)
            .ToList();

        var expectedNames = expectedNamesString.Split(';').ToHashSet();
        var actualNames   = new HashSet<string>();

        foreach (var stepFactory in factories)
        {
            actualNames.Add(stepFactory.TypeName);
        }

        actualNames.Should().BeEquivalentTo(expectedNames);
    }
}

}
