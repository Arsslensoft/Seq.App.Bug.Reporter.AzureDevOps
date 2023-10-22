using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Seq.App.Bug.Reporter.AzureDevOps.AzureDevOps;
using Seq.App.Bug.Reporter.AzureDevOps.Constants;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Bug.Reporter.AzureDevOps;

/// <summary>
/// Represents a base class for Azure DevOps Seq apps.
/// </summary>
public abstract class AzureDevOpsReporterAppBase : SeqApp
{
    /// <summary>
    /// Increments the incident frequency of an existing bug.
    /// </summary>
    /// <param name="client">The Azure DevOps client</param>
    /// <param name="existingWorkItem">The existing work item to update</param>
    /// <param name="logEvent">The log event.</param>
    protected async Task IncrementIncidentFrequencyAsync(AzureDevOpsClient client, WorkItemReference existingWorkItem,
        Event<LogEventData> logEvent)
    {
        var workItem = await client.GetWorkItemAsync(Project, existingWorkItem.Id);

        // Ignore if its a duplicate seq event
        if (!string.IsNullOrEmpty(SeqEventIdField) && workItem.Fields.TryGetValue(SeqEventIdField, out var eventId) &&
            eventId.ToString() == logEvent.Id)
            return;

        if (string.IsNullOrEmpty(IncidentFrequencyField))
            return;
        // Duplicate Issue by unique Id, increase frequency
        var isParsed = int.TryParse(workItem.Fields[IncidentFrequencyField].ToString(),
            out var frequency);

        // Update existing bug with new frequency
        var patchDocument = new JsonPatchDocument
        {
            new()
            {
                Operation = Operation.Replace,
                Path = "/fields/" + IncidentFrequencyField,
                Value = isParsed ? frequency + 1 : 1
            }
        };
        await client.UpdateWorkItemAsync(patchDocument, Project, existingWorkItem.Id);
    }

    #region Configuration

    /// <summary>
    /// Represents the organization name can be found within the base url of your Azure DevOps url.
    /// (Example: https://dev.azure.com/{your organization}/).
    /// </summary>
    [SeqAppSetting(DisplayName = "Azure DevOps Organization",
        HelpText =
            "The organization name can be found within the base url of your Azure DevOps url. (Example: https://dev.azure.com/{your organization}/).")]
    public required string Organization { get; set; }

    /// <summary>
    /// Represents the project name can be found within the base url of your Azure DevOps project url.
    /// (Example: https://dev.azure.com/{your organization}/{your project}).
    /// </summary>
    [SeqAppSetting(DisplayName = "Project",
        HelpText =
            "The project name can be found within the base url of your Azure DevOps project url. (Example: https://dev.azure.com/{your organization}/{your project}).")]
    public required string Project { get; set; }

    /// <summary>
    /// Represents the personal access token of your Azure DevOps account.
    /// (please configure your token to WorkItems/Read / Write).
    /// </summary>
    [SeqAppSetting(DisplayName = "Azure DevOps Personal Access Token",
        HelpText = "Azure DevOps Personal Access Token (please configure your token to WorkItems/Read & Write).")]
    public required string PersonalAccessToken { get; set; }

    /// <summary>
    /// Represents the description mapping field.
    /// If you're using CMMI use Microsoft.VSTS.CMMI.Symptom.
    /// If you're using Scrum you should use Microsoft.VSTS.TCM.ReproSteps.
    /// </summary>
    [SeqAppSetting(DisplayName = "Description Mapping Field",
        IsOptional = false,
        HelpText =
            "Description DevOps Mapping Field. If you're using CMMI use Microsoft.VSTS.CMMI.Symptom. If you're using Scrum you should use Microsoft.VSTS.TCM.ReproSteps.")]
    public required string DescriptionMappingField { get; set; }

    /// <summary>
    /// Represents the comma separated list of bug tags to apply to the created bug in Azure DevOps.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Tags",
        IsOptional = true,
        HelpText = "Comma separated list of bug tags to apply to the created bug in Azure DevOps.")]
    public string? Tags { get; set; }

    /// <summary>
    /// Represents the area Path of the Azure DevOps bug.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Area Path",
        IsOptional = true,
        HelpText = "Area Path of the Azure DevOps bug.")]
    public string? AreaPath { get; set; }

    /// <summary>
    /// Represents the iteration of the Azure DevOps bug.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Iteration",
        IsOptional = true,
        HelpText = "Iteration of the Azure DevOps bug.")]
    public string? Iteration { get; set; }

    /// <summary>
    /// Represents the person the work item should be assigned to.
    /// If left blank it will default to unassigned.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Assigned To",
        IsOptional = true,
        HelpText = "Who the work item should be assigned to. If left blank it will default to unassigned.")]
    public string? AssignedTo { get; set; }

    /// <summary>
    /// Represents the Azure DevOps custom field to store Seq Event Id.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Seq Event Id custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store Seq Event Id.")]
    public string? SeqEventIdField { get; set; }

    /// <summary>
    /// Represents the Azure DevOps custom field to store Seq Event Url.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Seq Event Url custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store Seq Event Url.")]
    public string? SeqEventUrlField { get; set; }

    /// <summary>
    /// Represents the Azure DevOps custom field to store the number of times this bug occurred and been logged in Seq.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Seq Event Incidence Count custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store the number of times this bug occurred and been logged in Seq.")]
    public string? IncidentFrequencyField { get; set; }

    /// <summary>
    /// Represents the Azure DevOps custom field to store the unique id of the bug.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Unique Incident Id custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store a unique incident id to prevent the creation of multiple bugs for the same incident, the unique id is a SHA-256 hash of the bug title.")]
    public string? IncidentUniqueIdField { get; set; }

    /// <summary>
    /// Represents the title format of the bug.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Bug title format",
        IsOptional = true,
        HelpText =
            $"Provides the title format of the bug. List of parameters that can be used are ({ParameterConstants.EventLogLevel}, {ParameterConstants.EventMessage}, {ParameterConstants.EventId}, {ParameterConstants.EventTimestamp}), you can also use Seq event properties). If left empty, the bug title will be '[SEQ Bug Reporter/{{EventLogLevel}}] - {{EventMessage}}'.")]
    public string? TitleFormat { get; set; }

    /// <summary>
    /// Represents the description format of the bug.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Bug description format",
        IsOptional = true,
        HelpText =
            $"Provides the description format of the bug. List of parameters that can be used are ({ParameterConstants.EventLogLevel}, {ParameterConstants.EventMessage}, {ParameterConstants.EventUrl}, {ParameterConstants.EventId}, {ParameterConstants.EventTimestamp}, {ParameterConstants.EventException}, {ParameterConstants.EventProperties}), you can also use Seq event properties).")]
    public string? DescriptionFormat { get; set; }

    /// <summary>
    /// Represents the parent work item id.
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Parent Work Item Id",
        IsOptional = true,
        HelpText =
            "Link to the parent related work item, if left blank the created bug will be un-parented.")]
    public string? WorkItemId { get; set; }

    /// <summary>
    /// Represents the mapping of Azure DevOps properties to constant values.
    /// Format: AzureDevOpsProperty1:ConstValue, AzureDevOpsProperty2:ConstValue
    /// Example: Priority:2,Triage:Level 1
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Azure DevOps props mappings",
        IsOptional = true,
        HelpText = "Maps Azure DevOps properties to constant values. Format: AzureDevOpsProperty:ConstValue " +
                   "Separated by Commas. " +
                   "Example: Priority:2,Triage:Level 1")]
    public string? DevOpsMappings { get; set; }

    /// <summary>
    /// Represents the mapping of Seq log levels to Azure DevOps bug severity.
    /// Format: LogLevel1:AzureDevOpsSeverity, LogLevel2:AzureDevOpsSeverity
    /// Example: Error:2 - High,Fatal:1 - Critical
    /// </summary>
    [SeqAppSetting(
        DisplayName = "Azure DevOps severity mappings",
        IsOptional = true,
        HelpText = "Maps Seq Log level to Azure DevOps bug severity. Format: LogLevel:AzureDevOpsSeverity " +
                   "Separated by Commas. " +
                   "Example: Error:2 - High,Fatal:1 - Critical")]
    public string? SeverityMappings { get; set; }

    #endregion
}