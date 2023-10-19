using Seq.Apps.LogEvents;
using Seq.Apps;

namespace Seq.App.BugReporter.AzureDevOps
{
    [SeqApp("Azure DevOps Bug Reporter",
        Description = "TBD.")]
    public class AzureDevOpsBugReporter : SeqApp, ISubscribeToAsync<LogEventData>
    {
        public async Task OnAsync(Event<LogEventData> evt)
        {
            throw new NotImplementedException();
        }
    }
}