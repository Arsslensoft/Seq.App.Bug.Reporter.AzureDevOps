using System.Text;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Seq.App.BugReporter.AzureDevOps.AzureDevOps;
using Seq.App.BugReporter.AzureDevOps.Builders;
using Seq.App.BugReporter.AzureDevOps.Extensions;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.BugReporter.AzureDevOps;

[SeqApp("Azure DevOps Bug Reporter",
    Description = "TBD.")]
public class AzureDevOpsBugReporterApp : SeqApp, ISubscribeToAsync<LogEventData>
{
    public async Task OnAsync(Event<LogEventData> evt)
    {
        if (evt.Data.Level != LogEventLevel.Error && evt.Data.Level != LogEventLevel.Fatal)
            return;
        try
        {
            var client = new AzureDevOpsClient(Organization, PersonalAccessToken);
            var existingWorkItems = new List<WorkItemReference>();
            var document = await CreateBugPayloadAsync(evt, client, existingWorkItems);
            switch (document)
            {
                case null when existingWorkItems.Count == 1 && !string.IsNullOrEmpty(IncidentFrequencyField):
                    await IncrementIncidentFrequencyAsync(client, existingWorkItems.First(), evt);
                    break;
                case null:
                    throw new ArgumentNullException(nameof(document), "Failed to build bug creation payload");
                default:
                    await client.CreateWorkItemAsync(document, Project);
                    break;
            }
        }
        catch (AggregateException aex)
        {
            var fex = aex.Flatten();
            throw new SeqAppException("Error while creating bug in Azure DevOps.", fex);
        }
        catch (Exception ex)
        {
            throw new SeqAppException("Error while creating bug in Azure DevOps.", ex);
        }
    }

    private async Task IncrementIncidentFrequencyAsync(AzureDevOpsClient client, WorkItemReference existingWorkItem, Event<LogEventData> evt)
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

    private async Task<JsonPatchDocument?> CreateBugPayloadAsync(Event<LogEventData> evt, AzureDevOpsClient client,
        List<WorkItemReference> existingWorkItems)
    {
        var builder = new JsonPatchDocumentWorkItemBuilder();

        var title = $"[SEQ Bug Reporter/{evt.Data.Level}][{{Environment}}] - {evt.Data.RenderedMessage}"
            .TruncateWithEllipsis(255);
        var description = GetDescription(evt);

        var uniqueId = title.GetStringHash();

        if (!string.IsNullOrEmpty(IncidentUniqueIdField) || !string.IsNullOrEmpty(SeqEventIdField))
        {
            var properties = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(IncidentUniqueIdField))
                properties.Add(IncidentUniqueIdField, uniqueId);
           
            if (!string.IsNullOrEmpty(SeqEventIdField))
                properties.Add(SeqEventIdField, evt.Id);

            var workItemQueryResult = await client.GetWorkItemByPropertyNameAsync(Project, properties, includeClosed: true);
            if (workItemQueryResult != null && workItemQueryResult.WorkItems.Count() != 0)
            {
                Log.Information("Duplicate DevOps item creation prevented for event id {id} / unique id {uniqueId}", evt.Id, uniqueId);
               
                if(!string.IsNullOrEmpty(IncidentFrequencyField))
                    existingWorkItems.AddRange(workItemQueryResult.WorkItems);

                return null;
            }
        }
        
        if (!string.IsNullOrEmpty(DevOpsMappings))
            builder.SetConstantProperties(DevOpsMappings.ParseKeyValueArray());

        builder
            .SetTitle(title)
            .SetAssignedTo(AssignedTo ?? string.Empty)
            .SetAreaPath(AreaPath)
            .SetIterationPath(Iteration)
            .SetEventFrequency(IncidentFrequencyField, 1)
            .SetSeqEventId(SeqEventIdField, evt.Id)
            .SetSeqEventUrl(SeqEventUrlField, GetSeqUrl(evt))
            .SetUniqueIdentifier(IncidentUniqueIdField, uniqueId)
            .SetTags(Tags)
            .LinkTo(WorkItemId, Organization, Project)
            .SetDescription(description, DescriptionMappingField)
            .SetSeverity(SeverityMappings, evt.Data.Level.ToString());

        return builder.Build();
    }

    private string GetDescription(Event<LogEventData> evt)
    {
        var sb = new StringBuilder();

        sb.Append($"<strong>Event Id:</strong> {evt.Id}<br/>");
        sb.Append($"<strong>Level:</strong> {evt.Data.Level}<br/>");
        sb.Append($"<strong>Timestamp:</strong> {evt.Data.LocalTimestamp.ToLocalTime()}<br/>");
        sb.Append($"<strong>Event Url:</strong> <a href=\"{GetSeqUrl(evt)}\" target=\"_blank\">Seq Event Url</a><br/>");

        foreach (var m in evt.Data.Properties.Keys) sb.Append($"<strong>{m}</strong>: {evt.Data.Properties[m]} <br/>");

        sb.Append($"<strong>Message:</strong> {evt.Data.RenderedMessage}<br/>");

        if (evt.Data?.Exception != null)
            sb.Append(
                $"<strong>Exception:</strong><p style=\"background-color: #921b3c; color: white; border-left: 8px solid #7b1e38;\">{evt.Data?.Exception}</p>");

        return sb.ToString();
    }

    private string GetSeqUrl(Event<LogEventData> evt)
    {
        return $"{Host.BaseUri}#/events?filter=@Id%20%3D%20'{evt.Id}'&show=expanded";
    }

    #region Configuration

    [SeqAppSetting(DisplayName = "Azure DevOps Organization",
        HelpText = "https://dev.azure.com/{your organization}/).")]
    public string Organization { get; set; }

    [SeqAppSetting(DisplayName = "Project",
        HelpText =
            "Project Name, this is the {your project} part of your project URL (Example: https://dev.azure.com/{your organization}/{your project}).")]
    public string Project { get; set; }

    [SeqAppSetting(DisplayName = "Azure DevOps Personal Access Token",
        HelpText = "Azure DevOps Personal Access Token (please configure your token to WorkItems/Read & Write).")]
    public string PersonalAccessToken { get; set; }

    //Microsoft.VSTS.CMMI.Symptom
    [SeqAppSetting(DisplayName = "Description Mapping Field",
        HelpText =
            "Description DevOps Mapping Field. For Bugs using CMMI this typically be Microsoft.VSTS.CMMI.Symptom, For CMMI Tasks it would be: System.Description. For Bugs in Scrum you might use Repro Steps: Microsoft.VSTS.TCM.ReproSteps")]
    public string DescriptionMappingField { get; set; }

    [SeqAppSetting(
        DisplayName = "Tags",
        IsOptional = true,
        HelpText = "Comma separated list of issue tags to apply to item in DevOps")]
    public string Tags { get; set; }

    [SeqAppSetting(
        DisplayName = "Area Path",
        IsOptional = true,
        HelpText = "Area Path of DevOps item")]
    public string AreaPath { get; set; }

    [SeqAppSetting(
        DisplayName = "Iteration",
        IsOptional = true,
        HelpText = "Iteration of the DevOps item")]
    public string Iteration { get; set; }

    [SeqAppSetting(
        DisplayName = "Assigned To",
        IsOptional = true,
        HelpText = "Who the work item should be assigned to. If left blank it will default to unassigned")]
    public string AssignedTo { get; set; }

    [SeqAppSetting(
        DisplayName = "Seq Event Id custom field # within DevOps",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store Seq Event Id. If provided will be used to prevent duplicate issue creation")]
    public string SeqEventIdField { get; set; }

    [SeqAppSetting(
        DisplayName = "Seq Event Url custom field # within DevOps",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store Seq Event Url. If provided will be used to put the seq event url")]
    public string SeqEventUrlField { get; set; }

    [SeqAppSetting(
        DisplayName = "Seq Event Incidence Count custom field # within DevOps",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store Seq Event Url. If provided will be used to put the seq event url")]
    public string IncidentFrequencyField { get; set; }

    [SeqAppSetting(
        DisplayName = "Unique Incident Id custom field # within DevOps",
        IsOptional = true,
        HelpText =
            "Azure DevOps custom field to store a unique incident id to prevent the creation of multiple bugs for the same incident, the UniqueIncidentIdKey field contains the format of the key. If provided, will not create a bug for each event when there is an active bug for the same message.")]
    public string IncidentUniqueIdField { get; set; }


    [SeqAppSetting(
        DisplayName = "Parent Link URL",
        IsOptional = true,
        HelpText =
            "Link to the parent related work item. If not defined it will be un-parented.")]
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
}