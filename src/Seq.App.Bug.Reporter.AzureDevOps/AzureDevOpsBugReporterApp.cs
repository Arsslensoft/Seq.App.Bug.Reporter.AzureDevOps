using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Seq.App.Bug.Reporter.AzureDevOps.AzureDevOps;
using Seq.App.Bug.Reporter.AzureDevOps.Builders;
using Seq.App.Bug.Reporter.AzureDevOps.Extensions;
using Seq.App.Bug.Reporter.AzureDevOps.Formatters;
using Seq.App.Bug.Reporter.AzureDevOps.Resources;
using Seq.Apps;
using Seq.Apps.LogEvents;

namespace Seq.App.Bug.Reporter.AzureDevOps;

/// <summary>
/// Represents the Azure DevOps bug reporter app.
/// </summary>
[SeqApp("Azure DevOps Bug Reporter",
    Description = "A Seq app that reports bugs to Azure DevOps.")]
public class AzureDevOpsBugReporterApp : AzureDevOpsReporterAppBase, ISubscribeToAsync<LogEventData>
{
    /// <inheritdoc />
    public async Task OnAsync(Event<LogEventData> logEvent)
    {
        if (logEvent.Data.Level != LogEventLevel.Error && logEvent.Data.Level != LogEventLevel.Fatal)
            return;
        try
        {
            var client = new AzureDevOpsClient(Organization, PersonalAccessToken);
            var existingWorkItems = new List<WorkItemReference>();
            var document = await CreateBugPayloadAsync(logEvent, client, existingWorkItems);
            switch (document)
            {
                case null when existingWorkItems.Count == 1 && !string.IsNullOrEmpty(IncidentFrequencyField):
                    await IncrementIncidentFrequencyAsync(client, existingWorkItems.First(), logEvent);
                    break;
                case null:
                    throw new ArgumentNullException(nameof(document), Strings.FAILED_TO_BUILD_BUG_CREATION_PAYLOAD);
                default:
                    await client.CreateBugAsync(document, Project);
                    break;
            }
        }
        catch (AggregateException aex)
        {
            var fex = aex.Flatten();
            throw new SeqAppException(Strings.BUG_CREATION_ERROR, fex);
        }
        catch (Exception ex)
        {
            throw new SeqAppException(Strings.BUG_CREATION_ERROR, ex);
        }
    }

    /// <summary>
    /// Creates the bug payload.
    /// </summary>
    /// <param name="logEvent">The log event</param>
    /// <param name="client">The Azure DevOps client</param>
    /// <param name="existingWorkItems">The list that contains the existing work items that the client found. This parameter should be initialized to an empty list and it's an output parameter</param>
    /// <returns>The <see cref="JsonPatchDocument"/> to be used in bug creation</returns>
    /// <exception cref="ArgumentNullException">Thrown if it failed the mapping of the title or description to their user defined/default formats</exception>
    protected async Task<JsonPatchDocument?> CreateBugPayloadAsync(Event<LogEventData> logEvent, AzureDevOpsClient client,
        List<WorkItemReference> existingWorkItems)
    {
        var builder = new JsonPatchDocumentWorkItemBuilder();
        var formatter = new ParameterizedSeqStringFormatter(logEvent, Log);

        var title = formatter.GetTitle(TitleFormat)?.TruncateWithEllipsis(255);
        if (title == null) throw new ArgumentNullException(nameof(title), Strings.FAILED_TO_GENERATE_TITLE);

        var description = formatter.GetDescription(Host.BaseUri, DescriptionFormat);
        if (description == null) throw new ArgumentNullException(nameof(description), Strings.FAILED_TO_GENERATE_DESCRIPTION);

        var uniqueId = title.GetStringHash();

        if (!string.IsNullOrEmpty(IncidentUniqueIdField) || !string.IsNullOrEmpty(SeqEventIdField))
        {
            var properties = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(IncidentUniqueIdField))
                properties.Add(IncidentUniqueIdField, uniqueId);

            if (!string.IsNullOrEmpty(SeqEventIdField))
                properties.Add(SeqEventIdField, logEvent.Id);

            var workItemQueryResult = await client.GetWorkItemByPropertyNameAsync(Project, properties, true);
            if (workItemQueryResult != null && workItemQueryResult.WorkItems.Any())
            {
                Log.Information(Strings.DUPLICATE_AZURE_DEVOPS_BUG,
                    logEvent.Id, uniqueId);

                if (!string.IsNullOrEmpty(IncidentFrequencyField))
                    existingWorkItems.AddRange(workItemQueryResult.WorkItems);

                return null;
            }
        }

        if (!string.IsNullOrEmpty(DevOpsMappings))
            builder.SetConstantProperties(DevOpsMappings.ParseKeyValueArray());

        builder
            .SetTitle(title)
            .SetAssignedTo(AssignedTo)
            .SetAreaPath(AreaPath)
            .SetIterationPath(Iteration)
            .SetEventFrequency(IncidentFrequencyField, 1)
            .SetSeqEventId(SeqEventIdField, logEvent.Id)
            .SetSeqEventUrl(SeqEventUrlField, formatter.GetSeqUrl(Host.BaseUri))
            .SetUniqueIdentifier(IncidentUniqueIdField, uniqueId)
            .SetTags(Tags)
            .LinkTo(WorkItemId, Organization, Project)
            .SetDescription(description, DescriptionMappingField)
            .SetSeverity(SeverityMappings, logEvent.Data.Level.ToString())
            .SetState(DefaultState);

        return builder.Build();
    }
}