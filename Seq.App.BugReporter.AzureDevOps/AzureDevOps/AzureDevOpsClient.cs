using System.Text;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Seq.App.BugReporter.AzureDevOps.AzureDevOps;

/// <summary>
/// Represents the Azure DevOps client.
/// </summary>
public class AzureDevOpsClient
{
    private readonly WorkItemTrackingHttpClient _client;

    /// <summary>
    /// Creates a new instance of <see cref="AzureDevOpsClient"/>.
    /// </summary>
    /// <param name="organization">The Azure DevOps organization</param>
    /// <param name="personalAccessToken">The Azure DevOps personal access token</param>
    public AzureDevOpsClient(string organization, string personalAccessToken)
    {
        var connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"),
            new VssBasicCredential(string.Empty, personalAccessToken));
        _client = connection.GetClient<WorkItemTrackingHttpClient>();
    }
    
    /// <summary>
    /// Creates a bug.
    /// </summary>
    /// <param name="patchDocument">The bug creation payload</param>
    /// <param name="project">The project</param>
    /// <returns>The created work item.</returns>
    public Task<WorkItem> CreateBugAsync(JsonPatchDocument patchDocument, string project)
    {
        return _client.CreateWorkItemAsync(patchDocument, project, "Bug", false, true);
    }

    /// <summary>
    /// Gets a work item.
    /// </summary>
    /// <param name="project">The project</param>
    /// <param name="workItemId">The work item id</param>
    /// <returns>The returned work item</returns>
    public async Task<WorkItem> GetWorkItemAsync(string project, int workItemId)
    {
        return await _client.GetWorkItemAsync(project, workItemId);
    }

    /// <summary>
    /// Updates a work item.
    /// </summary>
    /// <param name="patchDocument">The update payload</param>
    /// <param name="project">The project</param>
    /// <param name="workItemId">The work item id</param>
    /// <returns>The updated work item</returns>
    public async Task<WorkItem> UpdateWorkItemAsync(JsonPatchDocument patchDocument, string project, int workItemId)
    {
        return await _client.UpdateWorkItemAsync(patchDocument, project, workItemId);
    }

    /// <summary>
    /// Gets work items by query.
    /// </summary>
    /// <param name="query">The selection query</param>
    /// <returns>The returned work item references</returns>
    public async Task<WorkItemQueryResult> GetWorkItemsAsync(Wiql query)
    {
        return await _client.QueryByWiqlAsync(query);
    }

    /// <summary>
    /// Gets work items by properties.
    /// </summary>
    /// <param name="project">The project</param>
    /// <param name="properties">The properties to filter work items</param>
    /// <param name="includeClosed">Indicates whether are closed work items included or not</param>
    /// <returns>The returned work item references</returns>
    public async Task<WorkItemQueryResult?> GetWorkItemByPropertyNameAsync(string project,
        IEnumerable<KeyValuePair<string, string>> properties, bool includeClosed = false)
    {
        var keyValuePairs = properties as KeyValuePair<string, string>[] ?? properties.ToArray();

        if (keyValuePairs.IsNullOrEmpty()) return null;

        var sb = new StringBuilder();
        sb.Append("Select [State], [Title] From WorkItems Where ");
        sb.Append("(");

        var lastProperty = keyValuePairs.Last();
        foreach (var property in keyValuePairs)
        {
            sb.Append($"[{property.Key}] = '{property.Value}' ");

            if (lastProperty.Key != property.Key && lastProperty.Value != property.Value)
                sb.Append("Or ");
        }

        sb.Append(")");
        sb.Append($" And [System.TeamProject] = '{project}' ");

        if (includeClosed)
            sb.Append("[System.State] <> 'Closed' And ");

        sb.Append("Order By [State] Asc, [Changed Date] Desc");

        var query = new Wiql
        {
            Query = sb.ToString()
        };

        return await GetWorkItemsAsync(query);
    }
}