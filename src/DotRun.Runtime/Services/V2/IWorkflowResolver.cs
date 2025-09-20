using System.Collections.Generic;
using DotRun.Runtime.Model.V2;

namespace DotRun.Runtime.Services.V2;

public interface IWorkflowResolver
{
    /// <summary>
    /// Resolve a workflow by its path.
    /// Path can be: "workflow", "path/workflow", "repo/path/workflow"
    /// </summary>
    Workflow GetByPath(DotRunConfigV2 config, string workflowPath);
    
    /// <summary>
    /// List all available workflow paths
    /// </summary>
    IEnumerable<string> ListWorkflows(DotRunConfigV2 config);
    
    /// <summary>
    /// Check if a workflow exists at the given path
    /// </summary>
    bool ValidateWorkflow(DotRunConfigV2 config, string workflowPath);
    
    /// <summary>
    /// Get all workflows that match a given name (could be in different paths)
    /// </summary>
    IEnumerable<(string Path, Workflow Workflow)> FindWorkflows(DotRunConfigV2 config, string workflowName);
}