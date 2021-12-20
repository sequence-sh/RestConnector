using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Reductech.Sequence.ConnectorManagement.Base;
using Xunit;

namespace Reductech.Sequence.Connectors.Rest.Tests;

[AutoTheory.UseTestOutputHelper]
public partial class StepGenerationTests
{
    [Theory]
    [InlineData(
        nameof(SpecificationExamples.RevealJson),
        "RevealJson_Cases_Get",
        "InControlAuthToken"
    )]
    [InlineData(
        nameof(SpecificationExamples.RevealJson),
        "RevealJson_Cases_Delete",
        "id;InControlAuthToken"
    )]
    public void TestCreateParameters(
        string specificationName,
        string stepName,
        string expectedParameterNames)
    {
        var specificationText = SpecificationExamples.ResourceManager.GetString(specificationName)!;

        var factories = OpenAPISpecification.CreateStepFactories(
                specificationName,
                "http://baseURL",
                specificationText
            )
            .GetOrThrow();

        var expectedNames = expectedParameterNames.Split(';').ToHashSet();
        var actualNames   = new HashSet<string>();

        var step = factories.Single(x => x.TypeName == stepName);

        foreach (var stepParameter in step.ParameterDictionary.Values.Distinct())
        {
            actualNames.Add(stepParameter.Name);
        }

        actualNames.Should().BeEquivalentTo(expectedNames);
    }

    private const string ExpectedRevealSteps =
        "RevealJson_Archive_Get;RevealJson_Cases_Get;RevealJson_Cases_Put;RevealJson_Cases_Post;RevealJson_Cases_Delete;RevealJson_Cases_GetApiCasesByCaseId;RevealJson_Cases_RequestLoginToken;RevealJson_Cases_GetCaseStatistics;RevealJson_Cases_LookupLoginToken;RevealJson_Cases_GetCaseDatabaseTemplates;RevealJson_Clients_Get;RevealJson_Clients_Put;RevealJson_Clients_Post;RevealJson_Clients_Delete;RevealJson_Clients_GetApiClientsById;RevealJson_Company_Get;RevealJson_DataLoading_Get;RevealJson_DataLoading_LoadData;RevealJson_DataLoading_GetApiDataLoadingById;RevealJson_DataLoading_Delete;RevealJson_DataLoading_GetLogFile;RevealJson_Document_Post;RevealJson_Document_SetDocumentText;RevealJson_DocumentTextSet_GetAllTextSets;RevealJson_DocumentTextSet_UpdateTextSet;RevealJson_DocumentTextSet_CreateTextSet;RevealJson_DocumentTextSet_GetSingleTextSet;RevealJson_DocumentTextSet_DeleteTextSet;RevealJson_Field_GetAllFields;RevealJson_Field_UpdateField;RevealJson_Field_CreateField;RevealJson_Field_DeleteField;RevealJson_Field_GetFieldByName;RevealJson_Field_GetFieldById;RevealJson_Field_DeleteApiFieldById;RevealJson_Field_OverLayFields;RevealJson_Field_ClearField;RevealJson_Field_PutApiFieldClearById;RevealJson_Field_GetDocumentIds;RevealJson_Field_PostApiFieldDocumentIds;RevealJson_FieldMapping_Get;RevealJson_FieldMapping_ModifyFieldMapping;RevealJson_FieldMapping_CreateNewFieldMapping;RevealJson_FieldMapping_GetApiFieldMappingById;RevealJson_FieldMapping_DeleteFieldMapping;RevealJson_FieldProfile_AssociateWithFields;RevealJson_FieldProfile_Delete;RevealJson_FieldProfile_AssociateWithFields2;RevealJson_FieldProfile_UnAssociateWithFields;RevealJson_FieldProfile_Get;RevealJson_FieldProfile_Put;RevealJson_FieldProfile_Post;RevealJson_Groups_Get;RevealJson_ImageSet_Get;RevealJson_ImageSet_GetApiImageSet;RevealJson_ImageSet_Post;RevealJson_ImportBatch_Get;RevealJson_ImportBatch_GetDocumentIds;RevealJson_Indexing_GetIndexingJobs;RevealJson_Indexing_CreateIndexJob;RevealJson_Indexing_GetSingleIndexJob;RevealJson_Indexing_CancelIndexJob;RevealJson_Indexing_GetIndexes;RevealJson_Indexing_GetIndexingErrors;RevealJson_Indexing_GetIndexJobDocumentIds;RevealJson_JobDownloadManagement_Get;RevealJson_License_Get;RevealJson_License_Post;RevealJson_Login_Get;RevealJson_NexLP_GetNexLPJobs;RevealJson_NexLP_GetSingleNexLPJob;RevealJson_NexLP_GetNexLPJobDocumentIds;RevealJson_Processing_UpdateUploadJob;RevealJson_Processing_GetUploadJobReports;RevealJson_Processing_InsertUploadJobReport;RevealJson_Processing_DeleteUploadJobReports;RevealJson_Processing_GetUploadJobReport;RevealJson_S3Upload_GetS3Signature;RevealJson_S3Upload_PostManifest;RevealJson_TagPane_Create;RevealJson_TagPane_PostApiTagPaneItemCreate;RevealJson_TagPane_Delete;RevealJson_TagPane_GetTagPanes;RevealJson_TagPane_GetSingleTagSet;RevealJson_TagProfile_Create;RevealJson_TagProfile_Delete;RevealJson_TagProfile_GetTagProfiles;RevealJson_TagProfile_GetSingleTagSet;RevealJson_Tags_Get;RevealJson_Tags_GetDocumentIds;RevealJson_Tags_PostApiTagsDocumentIds;RevealJson_Tags_AddDocumentIds;RevealJson_Tags_DeleteDocumentIds;RevealJson_Tags_ClearDocumentIds;RevealJson_Tags_Create;RevealJson_Tags_Edit;RevealJson_Tags_PostApiTagsEdit;RevealJson_Tags_Delete;RevealJson_Tags_GetReviewedDocuments;RevealJson_TagSets_Create;RevealJson_TagSets_Edit;RevealJson_TagSets_Delete;RevealJson_TagSets_GetTagSets;RevealJson_TagSets_GetSingleTagSet;RevealJson_Team_Get;RevealJson_Team_Create;RevealJson_Team_Delete;RevealJson_Users_Get;RevealJson_Users_Put;RevealJson_Users_Post;RevealJson_Users_GetApiUsersById;RevealJson_Users_Delete;RevealJson_Version_Get;RevealJson_WordList_Get;RevealJson_WordList_Post;RevealJson_WorkFolder_GetRootFolder;RevealJson_WorkFolder_GetSubFolders;RevealJson_WorkFolder_GetDocumentIds;RevealJson_WorkFolder_AddDocumentIds;RevealJson_WorkFolder_DeleteDocumentIds;RevealJson_WorkFolder_ClearDocumentIds;RevealJson_WorkFolder_Create;RevealJson_WorkFolder_Edit;RevealJson_WorkFolder_Delete";

    private const string ExpectedOrchestratorStep =
        "Orchestrator_Bags_Get;Orchestrator_Bags_Post;Orchestrator_api_v_version_Bags_id_Get;Orchestrator_api_v_version_Bags_id_Patch;Orchestrator_api_v_version_Bags_id_Put;Orchestrator_api_v_version_Bags_id_Delete;Orchestrator_Bag_Sequences;Orchestrator_Logs_Get;Orchestrator_Runs_Get;Orchestrator_Runs_Post;Orchestrator_api_v_version_Runs_id_Get;Orchestrator_api_v_version_Runs_id_Patch;Orchestrator_api_v_version_Runs_id_Put;Orchestrator_api_v_version_Runs_id_Delete;Orchestrator_Sequence_Runs;Orchestrator_Sequences_Get;Orchestrator_Sequences_Post;Orchestrator_api_v_version_Sequences_id_Get;Orchestrator_api_v_version_Sequences_id_Patch;Orchestrator_api_v_version_Sequences_id_Put;Orchestrator_api_v_version_Sequences_id_Delete";

    [Theory]
    [InlineData(nameof(SpecificationExamples.Example),      "Example_users_Get")]
    [InlineData(nameof(SpecificationExamples.ExampleJson),  "ExampleJson_users_Get")]
    [InlineData(nameof(SpecificationExamples.RevealJson),   ExpectedRevealSteps)]
    [InlineData(nameof(SpecificationExamples.Orchestrator), ExpectedOrchestratorStep)]
    public void TestCreateFromSettings(string specificationName, string expectedNamesString)
    {
        var dsg = new DynamicStepGenerator();

        var specificationText = SpecificationExamples.ResourceManager.GetString(specificationName)!;

        var connectorSettings = new ConnectorSettings()
        {
            Id      = "Reductech.Sequence.Connectors.Rest",
            Enable  = true,
            Version = "1.0",
            Settings = new Dictionary<string, object>()
            {
                {
                    DynamicStepGenerator.SpecificationsKey,
                    new[]
                    {
                        new Dictionary<string, object>()
                        {
                            { "Name", specificationName },
                            { "BaseURL", "http://baseURL" },
                            { "Specification", specificationText }
                        }
                    }
                }
            }
        };

        var connectorSettingsJson = JsonSerializer.Serialize(connectorSettings);

        TestOutputHelper.WriteLine(connectorSettingsJson);

        var factories = dsg.TryCreateStepFactories(
                connectorSettings,
                null!
            )
            .GetOrThrow();

        var expectedNames = expectedNamesString.Split(';').ToHashSet();
        var actualNames   = new HashSet<string>();

        foreach (var stepFactory in factories)
        {
            actualNames.Add(stepFactory.TypeName);
        }

        actualNames.Should().BeEquivalentTo(expectedNames);
    }

    [Theory]
    [InlineData(nameof(SpecificationExamples.Example),      "Example_users_Get")]
    [InlineData(nameof(SpecificationExamples.ExampleJson),  "ExampleJson_users_Get")]
    [InlineData(nameof(SpecificationExamples.RevealJson),   ExpectedRevealSteps)]
    [InlineData(nameof(SpecificationExamples.Orchestrator), ExpectedOrchestratorStep)]
    public void TestFactoryNames(string specificationName, string expectedNamesString)
    {
        var specificationText = SpecificationExamples.ResourceManager.GetString(specificationName)!;

        var factories = OpenAPISpecification.CreateStepFactories(
                specificationName,
                "http://baseURL",
                specificationText
            )
            .GetOrThrow();

        var expectedNames = expectedNamesString.Split(';').ToHashSet();
        var actualNames   = new HashSet<string>();

        foreach (var stepFactory in factories)
        {
            actualNames.Add(stepFactory.TypeName);
        }

        actualNames.Should().BeEquivalentTo(expectedNames);
    }
}
