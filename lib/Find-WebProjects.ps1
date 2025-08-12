# Find-WebProjects.ps1
# Finds ASP.NET Core web projects and their child projects

function Find-WebProjects {
    param (
        [string]$RootPath,
        [string[]]$ExcludeKeywords = @()
    )

    Debug-Log "Searching for ASP.NET Core web projects in $RootPath..."

    # Find all .csproj files
    $allCsprojFiles = Get-ChildItem -Path $RootPath -Recurse -Filter *.csproj

    if ($ExcludeKeywords.Count -gt 0) {
        $allCsprojFiles = $allCsprojFiles | Where-Object {
            $pathLower = $_.FullName.ToLower()
            -not ($ExcludeKeywords | ForEach-Object { $pathLower.Contains($_.ToLower()) } | Where-Object { $_ })
        }
    }

    $webProjects = @()

    foreach ($proj in $allCsprojFiles) {
        $projPath = $proj.FullName
        # Load csproj XML
        [xml]$projXml = Get-Content $projPath

        # Check if project is SDK-style and references Microsoft.AspNetCore.App or Microsoft.NET.Sdk.Web
        $sdkAttribute = $projXml.Project.Sdk

        $isWebSdk = $false
        if ($sdkAttribute -and $sdkAttribute -match "Microsoft.NET.Sdk.Web") {
            $isWebSdk = $true
        } else {
            # Alternatively check PackageReference for Microsoft.AspNetCore.App
            $packageRefs = $projXml.Project.ItemGroup.PackageReference | ForEach-Object { $_.Include }
            if ($packageRefs -contains "Microsoft.AspNetCore.App") {
                $isWebSdk = $true
            }
        }

        if ($isWebSdk) {
            $webProjects += $projPath
        }
    }

    Debug-Log "Found $($webProjects.Count) web projects."

    # For each web project, find all referenced projects recursively
    $groups = @()

    foreach ($webProj in $webProjects) {
        $visited = [System.Collections.Generic.HashSet[string]]::new()
        $allProjects = Get-ChildProjectsRecursive -ProjectPath $webProj -Visited $visited

        $groups += [PSCustomObject]@{
            RootProject = $webProj
            AllProjects = $allProjects
        }
    }

    return $groups
}

function Get-ChildProjectsRecursive {
    param (
        [string]$ProjectPath,
        [System.Collections.Generic.HashSet[string]]$Visited
    )

    if ($Visited.Contains($ProjectPath)) {
        return @()
    }

    $Visited.Add($ProjectPath) | Out-Null

    # Load .csproj XML
    [xml]$projXml = Get-Content $ProjectPath

    $projectReferences = @()
    foreach ($itemGroup in $projXml.Project.ItemGroup) {
        if ($itemGroup.ProjectReference) {
            $projectReferences += $itemGroup.ProjectReference
        }
    }

    # Collect referenced projects
    $refs = $projectReferences | ForEach-Object {
        # Path is relative to project file
        $refPath = Join-Path (Split-Path $ProjectPath) $_.Include
        (Resolve-Path $refPath).Path
    }

    $allProjects = @($ProjectPath)

    foreach ($ref in $refs) {
        $allProjects += Get-ChildProjectsRecursive -ProjectPath $ref -Visited $Visited
    }

    return $allProjects
}
