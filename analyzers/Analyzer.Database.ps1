# Analyzer.Database.ps1
# Analyzer to find database usage facts in projects

function Invoke-Analyzer-Database {
    param (
        [string[]]$Projects
    )

    Debug-Log "Analyzing database usage in projects..."

    $facts = @()

    foreach ($projPath in $Projects) {

        [xml]$projXml = Get-Content $projPath

        # Look for connection strings in project (e.g. appsettings.json or config files referenced)
        # Since analyzing csproj only can't find connection strings,
        # we check common patterns: PackageReferences to EFCore or Dapper, and presence of appsettings.json

        $pkgRefs = $projXml.Project.ItemGroup.PackageReference | ForEach-Object {
            $_.Include
        }

        # Identify DB library
        foreach ($pkg in $pkgRefs) {
            if ($pkg -match "EntityFrameworkCore") {
                $facts += @{Type="DBLibrary"; Value="EntityFrameworkCore"}
            } elseif ($pkg -match "Dapper") {
                $facts += @{Type="DBLibrary"; Value="Dapper"}
            } elseif ($pkg -match "Npgsql") {
                $facts += @{Type="DBLibrary"; Value="Npgsql (PostgreSQL)"}
            }
        }

        # Try to locate appsettings.json or appsettings.*.json to parse connection strings
        $projectDir = Split-Path $projPath

        $configFiles = Get-ChildItem -Path $projectDir -Filter "appsettings*.json" -ErrorAction SilentlyContinue

        foreach ($configFile in $configFiles) {
            try {
                $json = Get-Content $configFile.FullName -Raw | ConvertFrom-Json
                if ($json.ConnectionStrings) {
                    foreach ($connName in $json.ConnectionStrings.PSObject.Properties.Name) {
                        $connString = $json.ConnectionStrings.$connName
                        # Simple parse for server and database names from connection string
                        if ($connString -match "Server=([^;]+)") {
                            $server = $Matches[1]
                            $facts += @{Type="DBServer"; Value=$server}
                        }

                        if ($connString -match "Database=([^;]+)") {
                            $database = $Matches[1]
                            $facts += @{Type="DBName"; Value=$database}
                        }
                    }
                }
            } catch {
                Write-Host "Failed to parse JSON file $($configFile.FullName): $_"
            }
        }
    }

    if ($facts.Count -eq 0) {
        Write-Host "No database facts found."
        return $null
    }

    return [PSCustomObject]@{
        AnalyzerName = "Database"
        Facts = $facts
    }
}
