<#
.SYNOPSIS
  Build project reference graph and helpers.

.DESCRIPTION
  This script provides functions to:
    - Build a hashtable graph of csproj -> ProjectReference list
    - Resolve ProjectReference relative paths to absolute paths
    - Compute transitive closure (DFS) of project references
    - Build groups for web projects and optionally deduplicate them
#>

# Build a graph of .csproj -> [ referenced .csproj full paths ]
function Build-ProjectGraph {
    param([string]$RootPath)

    # Find all .csproj files under the given root
    $csprojFiles = Get-ChildItem -Path $RootPath -Filter *.csproj -Recurse -File -ErrorAction SilentlyContinue
    $graph = @{}

    foreach ($proj in $csprojFiles) {
        $refs = Get-ProjectReferences -ProjectFile $proj.FullName
        $graph[$proj.FullName] = $refs
    }

    return $graph
}

# Extracts ProjectReference Include paths and resolves them to absolute paths
function Get-ProjectReferences {
    param([string]$ProjectFile)

    # Returns array of full paths (strings). If none found returns empty array.
    $refs = @()
    $content = Get-Content -Path $ProjectFile -Raw -ErrorAction SilentlyContinue
    if (-not $content) { return $refs }

    # Match ProjectReference Include="...". This is a simple regex-based extraction.
    $matches = [regex]::Matches($content, '<ProjectReference\s+[^>]*Include\s*=\s*"([^"]+)"', [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
    foreach ($m in $matches) {
        $relPath = $m.Groups[1].Value
        $fullPath = Resolve-ProjectPath -BasePath (Split-Path $ProjectFile) -RelativePath $relPath
        if ($fullPath) { $refs += $fullPath }
    }
    return $refs
}

# Resolve a relative project path to an absolute filesystem path.
function Resolve-ProjectPath {
    param(
        [string]$BasePath,
        [string]$RelativePath
    )

    # Combine base + relative and try to resolve. Return $null if not found.
    try {
        $combined = Join-Path -Path $BasePath -ChildPath $RelativePath
        return (Resolve-Path -Path $combined -ErrorAction Stop).ProviderPath
    } catch {
        Write-Verbose "Project reference not found/resolved: $RelativePath (base: $BasePath)"
        return $null
    }
}

# Build groups: for each web project compute transitive closure of references.
function Build-ProjectGroups {
    param(
        [string[]]$WebProjects,
        [hashtable]$Graph,
        [switch]$Deduplicate
    )

    $groups = @()
    foreach ($wp in $WebProjects) {
        $closure = Get-TransitiveClosure -ProjectPath $wp -Graph $Graph
        $groups += [pscustomobject]@{ WebProject = $wp; Projects = $closure }
    }

    if ($Deduplicate) {
        $groups = Remove-DuplicateGroups -Groups $groups
    }

    return $groups
}

# Compute the transitive closure of project references using DFS.
function Get-TransitiveClosure {
    param(
        [string]$ProjectPath,
        [hashtable]$Graph
    )

    # Use a HashSet to avoid duplicates and a Stack for DFS.
    # HashSet keeps unique project paths and offers fast containment checks.
    $visited = New-Object 'System.Collections.Generic.HashSet[string]'
    $stack   = New-Object 'System.Collections.Stack'

    # Push initial project
    $stack.Push($ProjectPath)

    while ($stack.Count -gt 0) {
        $cur = $stack.Pop()
        # Add returns $true if newly added, but we ignore return value
        if (-not $visited.Contains($cur)) {
            $visited.Add($cur) | Out-Null

            # If current project has references in graph, push them for traversal
            if ($Graph.ContainsKey($cur)) {
                foreach ($r in $Graph[$cur]) {
                    if (-not $visited.Contains($r)) {
                        $stack.Push($r)
                    }
                }
            }
        }
    }

    # Return an array of visited items.
    # Defensive approach:
    #   - If $visited is a collection (and not a string), enumerate it to an array.
    #   - If something unexpected happened and $visited is a string, wrap it as single-element array.
    if ($visited -is [System.Collections.IEnumerable] -and -not ($visited -is [string])) {
        # For collections (HashSet etc.) wrap enumeration in array to force materialization
        return @($visited)
    } else {
        # Fallback: return single element array (avoid enumerating a string into chars)
        return ,$visited
    }
}

# Remove duplicate groups that have identical project sets.
function Remove-DuplicateGroups {
    param([object[]]$Groups)

    $seen = @{}
    $unique = @()
    foreach ($g in $Groups) {
        # Build a deterministic key by sorting project paths
        $key = ($g.Projects | Sort-Object) -join ';;'
        if (-not $seen.ContainsKey($key)) {
            $seen[$key] = $true
            $unique += $g
        }
    }
    return $unique
}
