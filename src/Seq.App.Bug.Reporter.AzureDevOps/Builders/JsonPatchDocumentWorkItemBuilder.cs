using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Seq.App.Bug.Reporter.AzureDevOps.Extensions;

namespace Seq.App.Bug.Reporter.AzureDevOps.Builders;

/// <summary>
/// Represents the work item builder.
/// </summary>
public class JsonPatchDocumentWorkItemBuilder
{
    private readonly JsonPatchDocument _patchDocument = new();

    /// <summary>
    /// Sets the title of the bug.
    /// </summary>
    /// <param name="title">The title</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetTitle(string title)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.Title",
            Value = title
        });
        return this;
    }

    /// <summary>
    /// Sets the description of the bug.
    /// </summary>
    /// <param name="description">The description of the bug</param>
    /// <param name="mappedField">The mapped description field</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetDescription(string description, string mappedField)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.Description",
            Value = description
        });

        if (!string.IsNullOrEmpty(mappedField))
            _patchDocument.Add(
                new JsonPatchOperation
                {
                    Path = $"/fields/{mappedField}",
                    Operation = Operation.Add,
                    Value = description
                });

        return this;
    }

    /// <summary>
    /// Sets the Azure DevOps properties of the bug to their constant mapped values.
    /// </summary>
    /// <param name="pairs">The property value pairs</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetConstantProperties(IEnumerable<KeyValuePair<string, string>> pairs)
    {
        foreach (var pair in pairs)
            _patchDocument.Add(new JsonPatchOperation
            {
                Operation = Operation.Add,
                Path = $"/fields/{pair.Key}",
                Value = pair.Value
            });

        return this;
    }

    /// <summary>
    /// Sets the bug tags.
    /// </summary>
    /// <param name="tags">The tags that are comma separated</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetTags(string? tags)
    {
        if (string.IsNullOrEmpty(tags)) return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.Tags",
            Value = tags
        });
        return this;
    }

    /// <summary>
    /// Links the bug to the parent work item.
    /// </summary>
    /// <param name="workItemId">The parent work item id</param>
    /// <param name="organization">The organization</param>
    /// <param name="project">The project</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder LinkTo(string? workItemId, string organization, string project)
    {
        if (string.IsNullOrEmpty(workItemId) || !int.TryParse(workItemId, out var id)) return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/relations/-",
            Value = new
            {
                rel = "System.LinkTypes.Hierarchy-Reverse",
                url = $"https://dev.azure.com/{organization}/{project}/_apis/wit/workItems/{id}"
            }
        });

        return this;
    }

    /// <summary>
    /// Sets the area path of the bug.
    /// </summary>
    /// <param name="areaPath">The area path</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetAreaPath(string? areaPath)
    {
        if (string.IsNullOrEmpty(areaPath)) return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.AreaPath",
            Value = areaPath
        });
        return this;
    }

    /// <summary>
    /// Sets the iteration path of the bug.
    /// </summary>
    /// <param name="iterationPath">The iteration path</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetIterationPath(string? iterationPath)
    {
        if (string.IsNullOrEmpty(iterationPath)) return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.IterationPath",
            Value = iterationPath
        });
        return this;
    }

    /// <summary>
    /// Sets the assigned to field of the bug.
    /// </summary>
    /// <param name="assignedTo">The person you would want to assign the bug to</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetAssignedTo(string? assignedTo)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.AssignedTo",
            Value = assignedTo ?? string.Empty
        });
        return this;
    }

    /// <summary>
    /// Sets the severity of the bug.
    /// </summary>
    /// <param name="severityMappings">The log-level/severity mapping</param>
    /// <param name="logLevel">The current log event level</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetSeverity(string? severityMappings, string logLevel)
    {
        if (string.IsNullOrEmpty(severityMappings)) return this;

        var mappings = severityMappings.ParseKeyValueArray();

        if (!mappings.TryGetValue(logLevel, out var severity))
            return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/Microsoft.VSTS.Common.Severity",
            Value = severity
        });
        return this;
    }

    /// <summary>
    /// Sets the Seq event id.
    /// </summary>
    /// <param name="seqEventIdPropertyName">The Azure DevOps Seq event id field name</param>
    /// <param name="eventId">The log event id</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetSeqEventId(string? seqEventIdPropertyName, string eventId)
    {
        return SetFieldValue(seqEventIdPropertyName, eventId);
    }

    /// <summary>
    /// Sets the unique identifier of the bug.
    /// </summary>
    /// <param name="uniqueIdentifierPropertyName">The Azure DevOps unique identifier field name</param>
    /// <param name="uniqueId">The unique identifier</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetUniqueIdentifier(string? uniqueIdentifierPropertyName, string uniqueId)
    {
        return SetFieldValue(uniqueIdentifierPropertyName, uniqueId);
    }

    /// <summary>
    /// Sets the event frequency of the bug.
    /// </summary>
    /// <param name="eventFrequencyPropertyName">The Azure DevOps event frequency field name</param>
    /// <param name="eventFrequency">The event frequency</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetEventFrequency(string? eventFrequencyPropertyName, int eventFrequency)
    {
        return SetFieldValue(eventFrequencyPropertyName, eventFrequency.ToString());
    }

    /// <summary>
    /// Sets the Seq event url.
    /// </summary>
    /// <param name="seqEventUrlPropertyName">The Azure DevOps Seq event url field name</param>
    /// <param name="url">The Seq event url</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetSeqEventUrl(string? seqEventUrlPropertyName, string url)
    {
        return SetFieldValue(seqEventUrlPropertyName, url);
    }

    /// <summary>
    /// Sets the Bug state.
    /// </summary>
    /// <param name="state">The bug state</param>
    /// <returns>The current builder</returns>
    public JsonPatchDocumentWorkItemBuilder SetState(string? state)
    {
        if (string.IsNullOrEmpty(state)) return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.State",
            Value = state
        });
        return this;
    }

    /// <summary>
    /// Builds the <see cref="JsonPatchDocument"/>.
    /// </summary>
    /// <returns>The final document</returns>
    public JsonPatchDocument Build()
    {
        return _patchDocument;
    }

    private JsonPatchDocumentWorkItemBuilder SetFieldValue(string? propertyName, string value)
    {
        if (string.IsNullOrEmpty(propertyName)) return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = $"/fields/{propertyName}",
            Value = value
        });

        return this;
    }
}