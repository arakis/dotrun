## WorkflowResolver Rewrite Plan

### Core Methods

1. **ListWorkflowMountPaths() → List<(string MountPath, WorkflowReference Reference)>**
   - Iterate through all mount points using `config.GetAllMountPoints()`
   - Scan each mount's physical path for workflow files
   - Return tuples of:
     - MountPath: path relative to the mount (e.g., "workflow", "subdir/workflow")  
     - Reference: the workflow's WorkflowReference
   - Same workflow can appear multiple times if accessible via different mounts

2. **ListWorkflowReferences() → List<WorkflowReference>**
   - Iterate directly over `config.Repositories`
   - Scan each repository's `BasePath` for workflow files
   - Build WorkflowReference objects:
     - Repository: repository name
     - Path: subdirectory path within repository (nullable)
     - Workflow: workflow name without extension
   - Return unique qualified references
   - **NEVER touches mount points** - pure repository structure traversal

3. **GetWorkflowByMountPath(string mountPath) → (Workflow, WorkflowReference)**
   - Input: mount-relative path like "workflow" or "subdir/workflow"
   - Iterate through mount points to find matching workflow
   - Load the Workflow object
   - Construct its WorkflowReference
   - Return both

4. **GetWorkflowByReference(WorkflowReference reference) → Workflow**
   - Find repository by `reference.Repository` in config
   - Build physical path: `repo.BasePath` + `reference.Path` + `reference.Workflow` + extension
   - Load and return Workflow
   - **NEVER iterates mount points** - direct path resolution

### Implementation Strategy

1. **Complete separation**:
   - Mount methods: Use `GetAllMountPoints()`, search via mount iteration
   - Reference methods: Use repository `BasePath` directly, no mount involvement

2. **Clean path resolution**:
   - Remove all old complex resolution logic
   - Mount paths: iterate and search
   - References: direct path construction from repository structure

3. **Workflow validation**:
   - Check for `kind: workflow` in YAML content
   - Support both `.yaml` and `.yml` extensions

### Code Structure
- Complete rewrite of `WorkflowResolver.cs` (keep same class name)
- Update `IWorkflowResolver` interface with new method signatures
- Use existing `WorkflowReference` and `Workflow` models