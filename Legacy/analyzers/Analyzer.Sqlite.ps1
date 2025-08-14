# Analyzer.Sqlite.ps1
# Analyzer to find SQLite usage facts in .NET projects

function Invoke-Analyzer-Sqlite {
    param (
        [string[]]$Projects
    )

    # Keywords for SQLite
    $DbKeywords = @{
        Packages = @(
            "Microsoft.Data.Sqlite",
            "System.Data.SQLite",
            "SQLitePCLRaw.bundle_e_sqlite3"
        )
        ConnKeys = @{
            DataSource = @("Data Source", "Filename", "DataSource")
        }
    }

    Debug-Log "Analyzing SQLite usage in projects..."

    $facts = @()

    foreach ($projPath in $Projects) {

        try {
            [xml]$projXml = Get-Content $projPath -ErrorAction Stop
        } catch {
            Write-Error "Failed to read project file: $projPath. $_"
            continue
        }

        $pkgRefs = $projXml.SelectNodes("//PackageReference") | ForEach-Object {
            $_.Include
        }

        foreach ($pkg in $pkgRefs) {
            foreach ($keyword in $DbKeywords.Packages) {
                if ($pkg -match [regex]::Escape($keyword)) {
                    $facts += @{Type="DBType"; Value="SQLite"}
                    $facts += @{Type="DBLibrary"; Value=$pkg}
                    break
                }
            }
        }

        if ($facts.Count -eq 0) {
            continue
        }

        $projectDir = Split-Path $projPath
        
        $validConfigs = @("appsettings.json", "appsettings.production.json", "appsettings.prod.json")
        $configFiles = Get-ChildItem -Path $projectDir -File -Filter "appsettings*.json" -ErrorAction SilentlyContinue |
            Where-Object { $validConfigs -contains $_.Name }

        foreach ($configFile in $configFiles) {
            try {
                $json = Get-Content $configFile.FullName -Raw -ErrorAction Stop | ConvertFrom-Json -ErrorAction Stop
            } catch {
                Write-Warning "Failed to parse JSON file $($configFile.FullName): $_"
                continue
            }

            if ($json.ConnectionStrings) {
                foreach ($connName in $json.ConnectionStrings.PSObject.Properties.Name) {
                    $connString = $json.ConnectionStrings.$connName

                    $dataSourceKeysPattern = ($DbKeywords.ConnKeys.DataSource -join "|")
                    if ($connString -match "($dataSourceKeysPattern)=([^;]+)") {
                        $filePath = $Matches[2]
                        $facts += @{Type="DBFilePath"; Value=$filePath}
                    }
                }
            }
        }
    }

    if ($facts.Count -eq 0) {
        Debug-Log "No SQLite facts found."
        return $null
    }

    return [PSCustomObject]@{
        AnalyzerName = "Database"
        Facts = $facts
    }
}
