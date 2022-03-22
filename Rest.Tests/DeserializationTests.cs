using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoTheory;
using MELT;
using Microsoft.Extensions.Logging;
using Moq;
using Reductech.Sequence.ConnectorManagement.Base;
using Reductech.Sequence.Core.Internal.Serialization;
using Reductech.Sequence.Core.TestHarness.Rest;
using RestSharp;
using Xunit.Abstractions;

namespace Reductech.Sequence.Connectors.Rest.Tests;

public static class TestCaseExtensions
{
    public static DeserializationTests.DeserializationTestInstance WithSettings(
        this DeserializationTests.DeserializationTestInstance testInstance,
        Dictionary<string, object> dictionary)
    {
        testInstance.ConnectorSettingsDict = dictionary;
        return testInstance;
    }
}

public partial class DeserializationTests
{
    public record DeserializationTestInstance : IAsyncTestInstance, ICaseWithSetup
    {
        public DeserializationTestInstance(string scl, params object[] expectedLoggedValues)
        {
            SCL                  = scl;
            ExpectedLoggedValues = expectedLoggedValues.Select(x => x.ToString()!).ToList();
        }

        private string SCL { get; }

        public string Name => SCL;

        private IReadOnlyCollection<string> ExpectedLoggedValues { get; }

        /// <inheritdoc />
        public async Task RunAsync(ITestOutputHelper testOutputHelper)
        {
            testOutputHelper.WriteLine(SCL);

            var assembly          = typeof(DynamicStepGenerator).Assembly;
            var connectorSettings = ConnectorSettings.DefaultForAssembly(assembly);
            connectorSettings.Settings = ConnectorSettingsDict;
            var connectorData = new ConnectorData(connectorSettings, assembly);

            var repository = new MockRepository(MockBehavior.Strict);

            var restClientFactory = RESTClientSetupHelper.GetRESTClientFactory(repository);

            var externalContext =
                ExternalContextSetupHelper.GetExternalContext(repository, restClientFactory);

            var stepFactoryStore =
                StepFactoryStore.TryCreate(externalContext, connectorData).GetOrThrow();

            var loggerFactory = TestLoggerFactory.Create();
            loggerFactory.AddXunit(testOutputHelper);

            var runner = new SCLRunner(
                loggerFactory.CreateLogger("Test"),
                stepFactoryStore,
                externalContext
            );

            var result = await runner.RunSequenceFromTextAsync(
                SCL,
                new Dictionary<string, object>(),
                CancellationToken.None
            );

            result.ShouldBeSuccessful();

            foreach (var finalCheck in FinalChecks)
            {
                finalCheck();
            }

            LogChecker.CheckLoggedValues(
                loggerFactory,
                LogLevel.Information,
                ExpectedLoggedValues
            );
        }

        /// <inheritdoc />
        public ExternalContextSetupHelper ExternalContextSetupHelper { get; } = new();

        public Dictionary<VariableName, ISCLObject> InjectedVariables { get; } = new();

        /// <inheritdoc />
        public RESTClientSetupHelper RESTClientSetupHelper { get; } = new();

        public Dictionary<string, object> ConnectorSettingsDict { get; set; } = new();

        /// <inheritdoc />
        public List<Action> FinalChecks { get; } = new();
    }

    public static DeserializationTestInstance WithOrchestratorExamples(
        DeserializationTestInstance dti)
    {
        return dti.WithSettings(
            new Dictionary<string, object>()
            {
                {
                    DynamicStepGenerator.SpecificationsKey, EntityConversionHelpers.ConvertToEntity(
                        new OpenAPISpecification(
                            "Orchestrator",
                            "http://orchestrator.com",
                            SpecificationExamples.Orchestrator,
                            null,
                            null,
                            null
                        )
                    )
                }
            }
        );
    }

    [GenerateAsyncTheory("Deserialize")]
    public IEnumerable<DeserializationTestInstance> TestCases
    {
        get
        {
            yield return WithOrchestratorExamples(
                    new DeserializationTestInstance(
                        "Orchestrator_api_v_version_Bags_id_Get Version:'1.0' id:'123'",
                        "('a': 1)"
                    )
                )
                .SetupHTTPSuccess(
                    "http://orchestrator.com",
                    ("/api/v1.0/Bags/123", Method.Get, null),
                    true,
                    HttpStatusCode.OK,
                    "{\"a\": 1}"
                );
        }
    }
}
