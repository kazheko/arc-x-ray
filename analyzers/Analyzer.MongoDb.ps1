# Analyzer.MongoDb.ps1
# Analyzer to find MongoDB usage facts in .NET projects

function Invoke-Analyzer-MongoDb {
    param (
        [string[]]$Projects
    )

    # Keywords for MongoDB
    $DbKeywords = @{
        Packages = @(
            "MongoDB.Driver"
        )
        ConnKeys = @{
            Server = @("Server", "Host", "Hostname")
            Database = @("Database")
        }
    }

    Debug-Log "Analyzing MongoDB usage in projects..."

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
                    $facts += @{Type="DBType"; Value="MongoDB"}
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

                    $serverKeysPattern = ($DbKeywords.ConnKeys.Server -join "|")
                    if ($connString -match "($serverKeysPattern)=([^;]+)") {
                        $server = $Matches[2]
                        $facts += @{Type="DBServer"; Value=$server}
                    }

                    $databaseKeysPattern = ($DbKeywords.ConnKeys.Database -join "|")
                    if ($connString -match "($databaseKeysPattern)=([^;]+)") {
                        $database = $Matches[2]
                        $facts += @{Type="DBName"; Value=$database}
                    }
                }
            }
        }
    }

    if ($facts.Count -eq 0) {
        Debug-Log "No MongoDB facts found."
        return $null
    }

    return [PSCustomObject]@{
        AnalyzerName = "Database"
        Facts = $facts
    }
}
