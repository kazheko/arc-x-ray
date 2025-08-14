# main.ps1
# Orchestrator script

param (
    [string]$RepoPath = ".",
    [string[]]$ExcludeKeywords = @()
)

# Import libraries
. "$PSScriptRoot/lib/Utils.ps1"
. "$PSScriptRoot/lib/Find-WebProjects.ps1"
. "$PSScriptRoot/lib/Load-Analyzers.ps1"
. "$PSScriptRoot/lib/Report-Generator.ps1"

Debug-Log "Starting analysis on repository: $RepoPath"
if ($ExcludeKeywords.Count -gt 0) {
    Debug-Log "Excluding projects containing keywords: $($ExcludeKeywords -join ', ')"
}

# Step 1: Find ASP.NET Core Web projects and their child projects with exclusion
$webProjectGroups = Find-WebProjects -RootPath $RepoPath -ExcludeKeywords $ExcludeKeywords

Debug-Log "Found $(@($webProjectGroups).Count) web project groups."

# Step 2: Load analyzers
$analyzers = Load-Analyzers -AnalyzersPath "$PSScriptRoot/analyzers"
Debug-Log "Loaded $(@($analyzers).Count) analyzers."

$allResults = @()

# Step 3: Run each analyzer on each web project group
foreach ($group in $webProjectGroups) {
    Debug-Log "Analyzing web service rooted at: $($group.RootProject)"

    $groupResults = @()

    foreach ($analyzer in $analyzers) {
        Debug-Log "Running analyzer: $($analyzer.Name)"
        $result = & $analyzer.InvokeFunction -Projects $group.AllProjects
        if ($result) {
            $groupResults += $result
        }
    }

    # Aggregate results per group
    $allResults += [PSCustomObject]@{
        RootProject = $group.RootProject
        Facts = $groupResults
    }
}

# Step 4: Generate report
if ($allResults.Count -eq 0) {
    Write-Host "No analysis results to report."
} else {
    Generate-Report -Results $allResults
}

Debug-Log "Analysis complete."
