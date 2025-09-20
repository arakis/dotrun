# Revised Plan: Hierarchical Config with Proper Path Handling

## Overview
Implement a 4-level hierarchy: `repo/path/project/workflow` with correct Unix path semantics.

## Key Concepts

### 1. Path Resolution Rules
- **Absolute paths** (start with `/` or `~`): Use as-is
- **Relative paths**: Relative to `Repository.BasePath`

### 2. Config YAML Structure
```yaml
SecretsFile: ~/dotrun.secrets.yaml

Repositories:
  - Name: local
    Type: FileSystem
    BasePath: ~/dotrun.config  # Base for relative paths
    Paths:
      - Name: dev              
        Path: projects         # RELATIVE: ~/dotrun.config/projects
        # Projects: null = auto-discover
        
      - Name: absolute
        Path: /opt/myprojects  # ABSOLUTE: /opt/myprojects
        
      - Name: home
        Path: ~/myprojects     # ABSOLUTE: ~/myprojects
        
  - Name: work
    Type: FileSystem
    BasePath: /var/repos      # Base for relative paths
    Paths:
      - Name: backend
        Path: services/backend  # RELATIVE: /var/repos/services/backend
        
      - Name: shared
        Path: /shared/libs      # ABSOLUTE: /shared/libs
        Projects:
          - Name: core
            Path: core-lib      # RELATIVE to Path: /shared/libs/core-lib
```

### 3. Path Resolution Logic
```csharp
string ResolvePath(Repository repo, RepositoryPath path) {
    if (IsAbsolutePath(path.Path)) {
        // Starts with / or ~
        return ExpandPath(path.Path);  // Expand ~ to home
    } else {
        // Relative path
        return Path.Combine(ExpandPath(repo.BasePath), path.Path);
    }
}

bool IsAbsolutePath(string path) {
    return path.StartsWith("/") || path.StartsWith("~");
}
```

### 4. Model Structure
```csharp
public class Repository {
    public string Name { get; set; }         
    public RepositoryType Type { get; set; } 
    public string BasePath { get; set; }     // Base for relative paths (optional if all paths are absolute)
    public List<RepositoryPath> Paths { get; set; }
}

public class RepositoryPath {
    public string Name { get; set; }         // Logical name: "dev"
    public string Path { get; set; }         // Physical: "projects" (relative) or "/opt/projects" (absolute)
    public List<RepositoryProject> Projects { get; set; } 
}

public class RepositoryProject {
    public string Name { get; set; }
    public string Path { get; set; }         // Always relative to RepositoryPath's resolved path
}
```

### 5. Example Resolution
```
Config:
  Repository: Name=local, BasePath=~/dotrun.config
  Path: Name=dev, Path=projects (relative)
  Project: Name=proj1 (auto-discovered)

Resolution for "local/dev/proj1":
1. Repository "local" → BasePath: ~/dotrun.config
2. Path "dev" → "projects" is relative → ~/dotrun.config/projects
3. Project "proj1" → ~/dotrun.config/projects/proj1

---

Config:
  Repository: Name=work, BasePath=/var/repos  
  Path: Name=external, Path=/opt/external (absolute)
  Project: Name=tool

Resolution for "work/external/tool":
1. Repository "work" → BasePath: /var/repos (ignored for this path)
2. Path "external" → "/opt/external" is absolute → /opt/external
3. Project "tool" → /opt/external/tool
```

### 6. Mixed Mode Example
```yaml
Repositories:
  - Name: mixed
    Type: FileSystem
    BasePath: ~/workspace
    Paths:
      - Name: local
        Path: projects          # ~/workspace/projects
      - Name: system  
        Path: /usr/local/share  # /usr/local/share (absolute)
      - Name: home
        Path: ~/personal        # ~/personal (absolute)
```

## Key Implementation Points
- `BasePath` is only used when `Path` is relative (no leading `/` or `~`)
- Project paths are always relative to their parent path (after resolution)
- Proper Unix path expansion for `~` to home directory

## Success Criteria
- `--project proj1` works with auto-discovery
- `--project local/dev/proj1` works with full qualification
- Both relative and absolute paths resolve correctly
- Mixed absolute/relative paths in same repository work