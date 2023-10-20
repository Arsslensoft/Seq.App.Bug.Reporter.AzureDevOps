using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Seq.App.BugReporter.AzureDevOps.AzureDevOps;
using Seq.App.BugReporter.AzureDevOps.Builders;
using Seq.App.BugReporter.AzureDevOps.Extensions;
using Seq.App.BugReporter.AzureDevOps.Formatters;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.BugReporter.AzureDevOps;

[SeqApp("Azure DevOps Bug Reporter",
    Description = "A Seq app that reports bugs to Azure DevOps.")]
public class AzureDevOpsBugReporterApp : AzureDevOpsReporterAppBase, ISubscribeToAsync<LogEventData>
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

     protected async Task<JsonPatchDocument?> CreateBugPayloadAsync(Event<LogEventData> evt, AzureDevOpsClient client,
        List<WorkItemReference> existingWorkItems)
    {
        var builder = new JsonPatchDocumentWorkItemBuilder();
        var formatter = new ParameterizedSeqStringFormatter(evt);

        var title = formatter.GetTitle(TitleFormat)?.TruncateWithEllipsis(255);
        if(title == null) throw new ArgumentNullException(nameof(title), "Failed to generate bug title");

        var description = formatter.GetDescription(Host.BaseUri, DescriptionFormat);
        if(description == null) throw new ArgumentNullException(nameof(description), "Failed to generate bug description");

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
            .SetSeqEventUrl(SeqEventUrlField, formatter.GetSeqUrl(Host.BaseUri))
            .SetUniqueIdentifier(IncidentUniqueIdField, uniqueId)
            .SetTags(Tags)
            .LinkTo(WorkItemId, Organization, Project)
            .SetDescription(description, DescriptionMappingField)
            .SetSeverity(SeverityMappings, evt.Data.Level.ToString());

        return builder.Build();
    }
}