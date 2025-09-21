using System.Threading.Tasks;
using DotRun.Runtime.Model;

namespace DotRun.Runtime.Services.Tracking;

public interface IWorkflowSyncService
{
    Task<WorkflowSyncResult> SyncWorkflowsAsync(DotRunConfigV2 config);
    Task<WorkflowSyncResult> SyncSingleWorkflowAsync(DotRunConfigV2 config, string qualifiedPath);
}

public class WorkflowSyncResult
{
    public int DiscoveredCount { get; set; }
    public int CreatedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int ReactivatedCount { get; set; }
    public int DeactivatedCount { get; set; }
    public List<string> Errors { get; set; } = new();
}