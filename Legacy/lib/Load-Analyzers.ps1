# Load-Analyzers.ps1
# Loads all analyzer scripts from the analyzers directory

function Load-Analyzers {
    param (
        [string]$AnalyzersPath
    )

    Debug-Log "Loading analyzers from $AnalyzersPath"

    $analyzerFiles = Get-ChildItem -Path $AnalyzersPath -Filter "Analyzer.*.ps1"

    $analyzers = @()

    foreach ($file in $analyzerFiles) {
        . $file.FullName

        # Assumes each analyzer script defines a function Invoke-Analyzer
        # We create an object with a name and scriptblock to call
        $name = ($file.BaseName -replace "^Analyzer\.", "")
        $invokeFunctionName = "Invoke-Analyzer-$name"
        if (Get-Command $invokeFunctionName -ErrorAction SilentlyContinue) {
            $analyzers += [PSCustomObject]@{
                Name = $name
                InvokeFunction = (Get-Command $invokeFunctionName).ScriptBlock
            }
        } else {
            Write-Warning "Analyzer function $invokeFunctionName not found in $($file.Name)"
        }
    }

    return $analyzers
}
