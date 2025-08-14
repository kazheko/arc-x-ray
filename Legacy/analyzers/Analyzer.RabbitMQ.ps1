# Analyzer.RabbitMQ.ps1
# Analyzer to find RabbitMQ usage facts in .NET projects

function Invoke-Analyzer-RabbitMQ {
    param (
        [string[]]$Projects
    )

    # Keywords for RabbitMQ
    $MqKeywords = @{
        Packages = @(
            "RabbitMQ.Client",
            "RawRabbit",
            "EasyNetQ",
            "MassTransit.RabbitMQ"
        )
        ConnKeys = @(
            "host", "hostname", "server" # Keys commonly found in config
        )
    }

    Debug-Log "Analyzing RabbitMQ usage in projects..."

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

        # Detect RabbitMQ libraries from packages
        foreach ($pkg in $pkgRefs) {
            foreach ($keyword in $MqKeywords.Packages) {
                if ($pkg -match [regex]::Escape($keyword)) {
                    $facts += @{Type="MQType"; Value="RabbitMQ"}
                    $facts += @{Type="MQLibrary"; Value=$pkg}
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

            # Check root-level properties for RabbitMQ settings
            foreach ($prop in $json.PSObject.Properties) {
                if ($prop.Name -match "Rabbit" -or $prop.Name -match "MQ") {
                    $value = $prop.Value
                    if ($null -ne $value -and ($value -is [string])) {
                        $facts += @{Type="RabbitMQConnectionString"; Value=$value}
                    }
                    elseif ($value -is [psobject]) {
                        foreach ($key in $MqKeywords.ConnKeys) {
                            if ($value.PSObject.Properties.Name -contains $key) {
                                $facts += @{Type="RabbitMQHost"; Value=$value.$key}
                            }
                        }
                    }
                }
            }

            # Check ConnectionStrings section for RabbitMQ connection strings
            if ($json.ConnectionStrings) {
                foreach ($connName in $json.ConnectionStrings.PSObject.Properties.Name) {
                    if ($connName -match "Rabbit" -or $connName -match "MQ") {
                        $connString = $json.ConnectionStrings.$connName
                        $facts += @{Type="RabbitMQConnectionString"; Value=$connString}
                    }
                }
            }
        }
    }

    if ($facts.Count -eq 0) {
        Debug-Log "No RabbitMQ facts found."
        return $null
    }

    return [PSCustomObject]@{
        AnalyzerName = "MessageQueue"
        Facts = $facts
    }
}
