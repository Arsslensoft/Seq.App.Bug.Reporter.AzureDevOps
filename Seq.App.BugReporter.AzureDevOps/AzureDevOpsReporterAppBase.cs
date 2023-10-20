using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Seq.App.BugReporter.AzureDevOps.AzureDevOps;
using Seq.App.BugReporter.AzureDevOps.Builders;
using Seq.App.BugReporter.AzureDevOps.Constants;
using Seq.App.BugReporter.AzureDevOps.Extensions;
using Seq.Apps;
using Seq.Apps.LogEvents;
public abstract class AzureDevOpsReporterAppBase : SeqApp 
{

    #region Configuration

    [SeqAppSetting(DisplayName = "Azure DevOps Organization",
        HelpText = "The organization name can be found within the base url of your Azure DevOps url. (Example: https://dev.azure.com/{your organization}/).")]
    public string Organization { get; set; }

    [SeqAppSetting(DisplayName = "Project",
        HelpText =
            "The project name can be found within the base url of your Azure DevOps project url. (Example: https://dev.azure.com/{your organization}/{your project}).")]
    public string Project { get; set; }

    [SeqAppSetting(DisplayName = "Azure DevOps Personal Access Token",
        HelpText = "Azure DevOps Personal Access Token (please configure your token to WorkItems/Read & Write).")]
    public string PersonalAccessToken { get; set; }

    [SeqAppSetting(DisplayName = "Description Mapping Field",
        HelpText =
            "Description DevOps Mapping Field. If you're using CMMI use Microsoft.VSTS.CMMI.Symptom. If you're using Scrum you should use Microsoft.VSTS.TCM.ReproSteps.")]
    public string DescriptionMappingField { get; set; }

    [SeqAppSetting(
        DisplayName = "Tags",
        IsOptional = true,
        HelpText = "Comma separated list of bug tags to apply to the created bug in Azure DevOps.")]
    public string Tags { get; set; }

    [SeqAppSetting(
        DisplayName = "Area Path",
        IsOptional = true,
        HelpText = "Area Path of the Azure DevOps bug.")]
    public string AreaPath { get; set; }

    [SeqAppSetting(
        DisplayName = "Iteration",
        IsOptional = true,
        HelpText = "Iteration of the Azure DevOps bug.")]
    public string Iteration { get; set; }

    [SeqAppSetting(
        DisplayName = "Assigned To",
        IsOptional = true,
        HelpText = "Who the work item should be assigned to. If left blank it will default to unassigned")]
    public string AssignedTo { get; set; }

    [SeqAppSetting(
        DisplayName = "Seq Event Id custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store Seq Event Id.")]
    public string SeqEventIdField { get; set; }

    [SeqAppSetting(
        DisplayName = "Seq Event Url custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store Seq Event Url.")]
    public string SeqEventUrlField { get; set; }

    [SeqAppSetting(
        DisplayName = "Seq Event Incidence Count custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store the number of times this bug occurred and been logged in Seq.")]
    public string IncidentFrequencyField { get; set; }

    [SeqAppSetting(
        DisplayName = "Unique Incident Id custom Azure DevOps field",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store a unique incident id to prevent the creation of multiple bugs for the same incident, the unique id is a SHA-256 hash of the bug title.")]
    public string IncidentUniqueIdField { get; set; }

    [SeqAppSetting(
        DisplayName = "Bug title format",
        IsOptional = true,
        HelpText = $"Provides the title format of the bug. List of parameters that can be used are ({ParameterConstants.EventLogLevel}, {ParameterConstants.EventMessage}, {ParameterConstants.EventId}, {ParameterConstants.EventTimestamp}), you can also use Seq event properties). If left empty, the bug title will be '[SEQ Bug Reporter/{{EventLogLevel}}] - {{EventMessage}}'.")]
    public string TitleFormat { get; set; }

    [SeqAppSetting(
        DisplayName = "Bug description format",
        IsOptional = true,
        HelpText = $"Provides the description format of the bug. List of parameters that can be used are ({ParameterConstants.EventLogLevel}, {ParameterConstants.EventMessage}, {ParameterConstants.EventUrl}, {ParameterConstants.EventId}, {ParameterConstants.EventTimestamp}, {ParameterConstants.EventException}, {ParameterConstants.EventProperties}), you can also use Seq event properties).")]
    public string DescriptionFormat { get; set; }

    [SeqAppSetting(
        DisplayName = "Parent Work Item Id",
        IsOptional = true,
        HelpText =
            "Link to the parent related work item, if left blank the created bug will be un-parented.")]
    public string WorkItemId { get; set; }

    [SeqAppSetting(
        DisplayName = "Azure DevOps props mappings",
        IsOptional = true,
        HelpText = "Maps Azure DevOps properties to constant values. Format: AzureDevOpsProperty:ConstValue " +
                   "Separated by Commas. " +
                   "Example: Priority:2,Triage:Level 1")]
    public string DevOpsMappings { get; set; }

    [SeqAppSetting(
        DisplayName = "Azure DevOps severity mappings",
        IsOptional = true,
        HelpText = "Maps Seq Log level to Azure DevOps bug severity. Format: LogLevel:AzureDevOpsSeverity " +
                   "Separated by Commas. " +
                   "Example: Error:2 - High,Fatal:1 - Critical")]
    public string SeverityMappings { get; set; }

    #endregion

    protected async Task IncrementIncidentFrequencyAsync(AzureDevOpsClient client, WorkItemReference existingWorkItem, Event<LogEventData> evt)
    {
        var workItem = await client.GetWorkItemAsync(Project, existingWorkItem.Id);

        // Ignore if its a duplicate seq event
        if (!string.IsNullOrEmpty(SeqEventIdField) && workItem.Fields.TryGetValue(SeqEventIdField, out var eventId) && eventId.ToString() == evt.Id)
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
}