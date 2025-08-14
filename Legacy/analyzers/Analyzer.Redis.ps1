# Analyzer.Redis.ps1
# Analyzer to find Redis usage facts in .NET projects

function Invoke-Analyzer-Redis {
    param (
        [string[]]$Projects
    )

    # Keywords for Redis
    $DbKeywords = @{
        Packages = @(
            "StackExchange.Redis",
            "Microsoft.Extensions.Caching.StackExchangeRedis",
            "ServiceStack.Redis",
            "Microsoft.Azure.CosmosDB.RedisCache"
        )
        ConnPattern = "^(?<host>[^:;]+):(?<port>\d+)" # Extract host:port pattern
    }

    Debug-Log "Analyzing Redis usage in projects..."

    $facts = @()

    foreach ($projPath in $Projects) {

        try {
            [xml]$projXml = Get-Content $projPath -ErrorAction Stop
        } catch {
            Write-Error "Failed to read project file: $projPath. $_"
            continue
        }

        # Find all referenced NuGet packages
        $pkgRefs = $projXml.SelectNodes("//PackageReference") | ForEach-Object {
            $_.Include
        }

        # Detect Redis libraries from packages
        foreach ($pkg in $pkgRefs) {
            foreach ($keyword in $DbKeywords.Packages) {
                if ($pkg -match [regex]::Escape($keyword)) {
                    $facts += @{Type="CacheType"; Value="Redis"}
                    $facts += @{Type="CacheLibrary"; Value=$pkg}
                    break
                }
            }
        }

        if ($facts.Count -eq 0) {
            continue
        }

        # Locate appsettings*.json files
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

            # Look for Redis settings in root-level properties
            foreach ($prop in $json.PSObject.Properties) {
                if ($prop.Name -match "Redis" -or $prop.Name -match "Cache") {
                    $value = $prop.Value
                    if ($null -ne $value -and ($value -is [string])) {
                        if ($value -match $DbKeywords.ConnPattern) {
                            $facts += @{Type="RedisHost"; Value=$Matches["host"]}
                            $facts += @{Type="RedisPort"; Value=$Matches["port"]}
                        }
                        else {
                            $facts += @{Type="RedisConnectionString"; Value=$value}
                        }
                    }
                }
            }

            # Look for Redis connection strings in ConnectionStrings section
            if ($json.ConnectionStrings) {
                foreach ($connName in $json.ConnectionStrings.PSObject.Properties.Name) {
                    if ($connName -match "Redis" -or $connName -match "Cache") {
                        $connString = $json.ConnectionStrings.$connName
                        if ($connString -match $DbKeywords.ConnPattern) {
                            $facts += @{Type="RedisHost"; Value=$Matches["host"]}
                            $facts += @{Type="RedisPort"; Value=$Matches["port"]}
                        }
                        else {
                            $facts += @{Type="RedisConnectionString"; Value=$connString}
                        }
                    }
                }
            }
        }
    }

    if ($facts.Count -eq 0) {
        Debug-Log "No Redis facts found."
        return $null
    }

    return [PSCustomObject]@{
        AnalyzerName = "Cache"
        Facts = $facts
    }
}
