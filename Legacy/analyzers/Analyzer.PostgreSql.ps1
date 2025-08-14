# Analyzer.PostgreSql.ps1
# Analyzer to find PostgreSQL usage facts in .NET projects

function Invoke-Analyzer-PostgreSql {
    param (
        [string[]]$Projects
    )

    # Keywords for PostgreSQL
    $DbKeywords = @{
        Packages = @(
            "Npgsql",
            "Npgsql.EntityFrameworkCore.PostgreSQL"
        )
        ConnKeys = @{
            Host = @("Host", "Server")
            Database = @("Database")
        }        
    }

    Debug-Log "Analyzing PostgreSQL usage in projects..."

    $facts = @()

    foreach ($projPath in $Projects) {

        try {
            [xml]$projXml = Get-Content $projPath -ErrorAction Stop
        } catch {
            Write-Error "Failed to read project file: $projPath. $_"
            continue
        }

        # Find all PackageReference nodes
        $pkgRefs = $projXml.SelectNodes("//PackageReference") | ForEach-Object {
            $_.Include
        }

        # Detect PostgreSQL libraries from packages
        foreach ($pkg in $pkgRefs) {
            foreach ($keyword in $DbKeywords.Packages) {
                if ($pkg -match [regex]::Escape($keyword)) {
                    $facts += @{Type="DBType"; Value="PostgreSQL"}
                    $facts += @{Type="DBLibrary"; Value=$pkg}
                    break
                }
            }
        }

        if ($facts.Count -eq 0) {
            continue
        }

        # Locate relevant appsettings*.json files
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

                    # Parse host/server
                    $hostKeysPattern = ($DbKeywords.ConnKeys.Host -join "|")
                    if ($connString -match "($hostKeysPattern)=([^;]+)") {
                        $server = $Matches[2]
                        $facts += @{Type="DBHost"; Value=$server}
                    }

                    # Parse database name
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
        Debug-Log "No PostgreSQL facts found."
        return $null
    }

    return [PSCustomObject]@{
        AnalyzerName = "Database"
        Facts = $facts
    }
}
