using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Seq.App.BugReporter.AzureDevOps.Extensions;

namespace Seq.App.BugReporter.AzureDevOps.Builders;

public class JsonPatchDocumentWorkItemBuilder
{
    private readonly JsonPatchDocument _patchDocument = new();

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

    public JsonPatchDocumentWorkItemBuilder SetReproSteps(string reproSteps)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/Microsoft.VSTS.TCM.ReproSteps",
            Value = reproSteps
        });
        return this;
    }

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

    public JsonPatchDocumentWorkItemBuilder SetCustomProperties(IEnumerable<KeyValuePair<string, object>> pairs)
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

    public JsonPatchDocumentWorkItemBuilder SetTags(string tags)
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

    public JsonPatchDocumentWorkItemBuilder SetAreaPath(string areaPath)
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

    public JsonPatchDocumentWorkItemBuilder SetIterationPath(string iterationPath)
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

    public JsonPatchDocumentWorkItemBuilder SetAssignedTo(string assignedTo)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.AssignedTo",
            Value = assignedTo
        });
        return this;
    }

    public JsonPatchDocumentWorkItemBuilder SetState(string state)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.State",
            Value = state
        });
        return this;
    }

    public JsonPatchDocumentWorkItemBuilder SetReason(string reason)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/System.Reason",
            Value = reason
        });
        return this;
    }

    public JsonPatchDocumentWorkItemBuilder SetSeverity(string severityMappings, string logLevel)
    {
        if (string.IsNullOrEmpty(severityMappings)) return this;

        var mappings = severityMappings.ParseKeyValueArray();
       
        if(!mappings.TryGetValue(logLevel, out var severity))
            return this;

        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/Microsoft.VSTS.Common.Severity",
            Value = severity
        });
        return this;
    }

    public JsonPatchDocumentWorkItemBuilder SetPriority(string priority)
    {
        _patchDocument.Add(new JsonPatchOperation
        {
            Operation = Operation.Add,
            Path = "/fields/Microsoft.VSTS.Common.Priority",
            Value = priority
        });
        return this;
    }

    public JsonPatchDocumentWorkItemBuilder SetSeqEventId(string seqEventIdPropertyName, string eventId)
    {
        return SetFieldValue(seqEventIdPropertyName, eventId);
    }

    public JsonPatchDocumentWorkItemBuilder SetUniqueIdentifier(string uniqueIdentifierPropertyName, string uniqueId)
    {
        return SetFieldValue(uniqueIdentifierPropertyName, uniqueId);
    }

    public JsonPatchDocumentWorkItemBuilder SetEventFrequency(string eventFrequencyPropertyName, int eventFrequency)
    {
        return SetFieldValue(eventFrequencyPropertyName, eventFrequency.ToString());
    }


    public JsonPatchDocumentWorkItemBuilder SetSeqEventUrl(string seqEventUrlPropertyName, string url)
    {
        return SetFieldValue(seqEventUrlPropertyName, url);
    }

    public JsonPatchDocumentWorkItemBuilder SetFieldValue(string propertyName, string value)
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

    public JsonPatchDocument Build()
    {
        return _patchDocument;
    }
}