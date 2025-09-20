# dotrun-config(5) - DotRun configuration file format

## NAME

dotrun-config - Configuration file for DotRun workflow orchestration

## SYNOPSIS

`~/dotrun.config/config.yaml`  
`~/dotrun.config/config-v2.yaml`

## DESCRIPTION

The DotRun configuration file defines repositories, mount paths, and workflow locations using a YAML format. It follows a mount-point model where directories containing workflows are mounted into a unified namespace.

## FILE FORMAT

The configuration file uses YAML syntax with the following top-level fields:

### SecretsFile

Path to the secrets file (optional). Supports tilde expansion.

```yaml
SecretsFile: ~/dotrun.secrets.yaml
```

### Repositories

List of repository definitions. Each repository contains workflow files and can have multiple mount paths.

```yaml
Repositories:
  - Name: <string>
    Type: <FileSystem|Git>
    BasePath: <path>
    Paths:
      - Mount: <string>
        Path: <path>
```

## FIELDS

### Repository Fields

**Name** (string, optional)  
Logical name for the repository. Empty string or omitted means global root namespace.

**Type** (enum, optional)  
Repository type. Currently supports:
- `FileSystem` (default) - Local filesystem
- `Git` (future) - Git repository

**BasePath** (path, optional)  
Base path for relative path resolution. Can be relative or absolute. Supports tilde expansion.

**Paths** (list, optional)  
List of mount points within the repository. If omitted, the entire BasePath is mounted at repository root.

### RepositoryPath Fields

**Mount** (string, optional)  
Mount point in the logical namespace. Can be:
- Relative mount: `dev` mounts under repository namespace (e.g., `repo/dev`)
- Absolute mount: `/tools` mounts at namespace root, ignoring repository name
- Empty string: mounts at repository root

**Path** (path, optional)  
Physical path to the workflow directory. Can be:
- Relative path: resolved relative to Repository.BasePath
- Absolute path: used as-is (starting with `/` or `~`)
- Default: `.` (current directory)

## PATH RESOLUTION

### Relative Paths
Paths not starting with `/` or `~` are relative to the repository's BasePath:

```yaml
Repositories:
  - Name: local
    BasePath: ~/dotrun.config
    Paths:
      - Mount: dev
        Path: projects/proj1  # Resolves to ~/dotrun.config/projects/proj1
```

### Absolute Paths
Paths starting with `/` or `~` are used as absolute paths, ignoring BasePath:

```yaml
Repositories:
  - Name: system
    BasePath: /ignored
    Paths:
      - Mount: global
        Path: /opt/workflows  # Uses /opt/workflows directly
      - Mount: user
        Path: ~/my-workflows  # Expands to home directory
```

## WORKFLOW RESOLUTION

Workflows are resolved using the following URI patterns:

- `workflow` - Search all mount points
- `path/workflow` - Search specific path in all repos
- `repo/path/workflow` - Fully qualified path

### Resolution Order

1. Direct resolution if fully qualified
2. Search in repository roots
3. Search in global root namespace
4. First match wins for ambiguous names

## NAMESPACE MERGING

Empty names create merged namespaces:

- Empty repository name → global root namespace
- Empty mount point → repository root
- All roots merge into a unified namespace

## ABSOLUTE MOUNTS

Mount points starting with `/` are absolute and ignore the repository name:

```yaml
Repositories:
  - Name: local
    BasePath: ~/workflows
    Paths:
      - Mount: /tools      # Accessible as: tools/workflow
        Path: shared-tools
      - Mount: /ci/builds  # Accessible as: ci/builds/workflow
        Path: ci-workflows
      - Mount: dev         # Accessible as: local/dev/workflow
        Path: development
```

Absolute mounts provide precise control over the namespace structure, allowing you to create standard locations like `/shared`, `/tools`, or `/ci` that are accessible without repository prefixes.

## EXAMPLES

### Minimal Configuration

```yaml
SecretsFile: ~/dotrun.secrets.yaml

Repositories:
  - BasePath: ~/workflows
```

### Simple Local Repository

```yaml
Repositories:
  - Name: local
    Type: FileSystem
    BasePath: ~/dotrun.config/projects
    Paths:
      - Mount: dev
        Path: proj1
      - Mount: test
        Path: proj2
```

### Multiple Repositories

```yaml
Repositories:
  # Named repository with multiple paths
  - Name: work
    BasePath: ~/company
    Paths:
      - Mount: backend
        Path: services/backend
      - Mount: frontend
        Path: services/frontend
        
  # Root namespace repository
  - BasePath: ~/personal/workflows
```

### Mixed Absolute and Relative Paths

```yaml
Repositories:
  - Name: mixed
    BasePath: ~/base
    Paths:
      - Mount: local
        Path: workflows       # ~/base/workflows
      - Mount: system
        Path: /opt/workflows  # /opt/workflows (absolute)
      - Mount: user
        Path: ~/custom       # ~/custom (expanded)
```

### Repository at Root Namespace

```yaml
Repositories:
  # Empty name merges into root
  - Name: ""
    BasePath: ~/workflows
    Paths:
      - Mount: ""
        Path: .  # Access as: workflow_name
```

### Advanced Multi-Environment Setup

```yaml
SecretsFile: ~/dotrun.secrets.yaml

Repositories:
  # Development workflows
  - Name: dev
    BasePath: ~/dev/workflows
    Paths:
      - Mount: ci
        Path: continuous-integration
      - Mount: cd
        Path: continuous-deployment
        
  # Production workflows
  - Name: prod
    BasePath: /opt/production
    Paths:
      - Mount: deploy
        Path: deployment
      - Mount: monitor
        Path: monitoring
        
  # Shared workflows in root namespace
  - BasePath: /usr/share/dotrun/workflows
```

## WORKFLOW ACCESS PATTERNS

Given the advanced setup above, workflows can be accessed as:

- `dev/ci/build` - Build workflow in dev CI path
- `prod/deploy/release` - Release workflow in production
- `utility` - Shared utility workflow from root namespace

## BACKWARD COMPATIBILITY

The system maintains backward compatibility by supporting:
- Simple project names when unambiguous
- Automatic directory scanning
- Progressive complexity (start simple, add structure as needed)

## MIGRATION PATH

1. Start with minimal config (single BasePath)
2. Add named repositories as organization grows
3. Add named paths for better structure
4. Use empty names to maintain simple access patterns

## FILES

`~/dotrun.config/config.yaml`  
Default configuration file location

`~/dotrun.config/config-v2.yaml`  
Version 2 configuration format

`~/dotrun.secrets.yaml`  
Default secrets file location

## SEE ALSO

dotrun(1), dotrun-workflow(5)

## AUTHORS

DotRun Development Team

## COPYRIGHT

This file is part of DotRun. Licensed under the MIT license.