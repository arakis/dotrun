using System.Collections.Generic;
using DotRun.Runtime.Model.V2;

namespace DotRun.Runtime.Services.V2;

public interface IWorkflowResolver
{
    /// <summary>
    /// Lists all workflow mount paths accessible via configured mount points.
    /// </summary>
    /// <param name="config">The DotRun configuration containing repository and mount definitions.</param>
    /// <returns>
    /// A list of tuples containing:
    /// - MountPath: The complete mount-relative path to access the workflow (e.g., "local/dev/proj1/demo")
    /// - Reference: The workflow's reference object with repository, path, and workflow name
    /// Same workflow can appear multiple times if accessible via different mounts.
    /// </returns>
    IList<(string MountPath, WorkflowReference Reference)> ListWorkflowMountPaths(DotRunConfigV2 config);

    /// <summary>
    /// Lists all unique workflow references by directly traversing repository base paths.
    /// </summary>
    /// <param name="config">The DotRun configuration containing repository definitions.</param>
    /// <returns>
    /// A list of unique WorkflowReference objects found across all repositories.
    /// This method never touches mount points - it performs pure repository structure traversal.
    /// </returns>
    IList<WorkflowReference> ListWorkflowReferences(DotRunConfigV2 config);

    /// <summary>
    /// Gets a workflow by its mount-relative path.
    /// </summary>
    /// <param name="config">The DotRun configuration containing repository and mount definitions.</param>
    /// <param name="mountPath">The mount-relative path (e.g., "workflow", "subdir/workflow", or "local/dev/workflow").</param>
    /// <returns>
    /// A tuple containing:
    /// - Workflow: The loaded workflow object, or null if not found
    /// - Reference: The workflow's reference object, or null if not found
    /// </returns>
    (Workflow Workflow, WorkflowReference Reference) GetWorkflowByMountPath(DotRunConfigV2 config, string mountPath);

    /// <summary>
    /// Gets a workflow by its WorkflowReference.
    /// </summary>
    /// <param name="config">The DotRun configuration containing repository definitions.</param>
    /// <param name="reference">The workflow reference containing repository, path, and workflow name.</param>
    /// <returns>
    /// The loaded workflow object, or null if not found.
    /// This method never iterates mount points - it uses direct path resolution from the repository base path.
    /// </returns>
    Workflow GetWorkflowByReference(DotRunConfigV2 config, WorkflowReference reference);
}