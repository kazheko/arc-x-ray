# Report-Generator.ps1
# Generates a report from analysis results

function Generate-Report {
    param (
        [Parameter(Mandatory)]
        [array]$Results
    )

    Write-Host "Generating report..."

    foreach ($groupResult in $Results) {
        Write-Host "Web Service: $((Split-Path $groupResult.RootProject -Leaf) -replace '\.csproj$','')"
        $allFacts = @{}

        foreach ($result in $groupResult.Facts) {
            foreach ($fact in $result.Facts) {
                # Use Type+Value as key to avoid duplicates
                $key = "$($fact.Type):$($fact.Value)"
                if (-not $allFacts.ContainsKey($key)) {
                    $allFacts[$key] = $fact.Value
                }
            }
        }

        Write-Host "  Found facts:"
        foreach ($key in $allFacts.Keys) {
            Write-Host "    $key"
        }

        Write-Host ""
    }
}
