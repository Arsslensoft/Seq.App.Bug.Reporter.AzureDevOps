using System.Text;
using Microsoft.TeamFoundation.Common;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace Seq.App.BugReporter.AzureDevOps.AzureDevOps;

public class AzureDevOpsClient
{
    public AzureDevOpsClient(string organization, string personalAccessToken)
    {
        Connection = new VssConnection(new Uri($"https://dev.azure.com/{organization}"),
            new VssBasicCredential(string.Empty, personalAccessToken));
        Client = Connection.GetClient<WorkItemTrackingHttpClient>();
    }

    public VssConnection Connection { get; set; }
    public WorkItemTrackingHttpClient Client { get; set; }

    public async Task<WorkItem> CreateWorkItemAsync(JsonPatchDocument patchDocument, string project)
    {
        return await Client.CreateWorkItemAsync(patchDocument, project, "Bug", false, true);
    }

    public async Task<WorkItem> GetWorkItemAsync(string project, int workItemId)
    {
        return await Client.GetWorkItemAsync(project, workItemId);
    }

    public async Task<WorkItem> UpdateWorkItemAsync(JsonPatchDocument patchDocument, string project, int workItemId)
    {
        return await Client.UpdateWorkItemAsync(patchDocument, project, workItemId);
    }

    public async Task<WorkItemQueryResult> GetWorkItemsAsync(Wiql query)
    {
        return await Client.QueryByWiqlAsync(query);
    }

    public async Task<WorkItemQueryResult?> GetWorkItemByPropertyNameAsync(string project, IEnumerable<KeyValuePair<string, string>> properties, bool includeClosed = false)
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