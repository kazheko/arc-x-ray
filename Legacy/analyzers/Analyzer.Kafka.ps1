# Analyzer.Kafka.ps1
# Analyzer to find Apache Kafka usage facts in .NET projects

function Invoke-Analyzer-Kafka {
    param (
        [string[]]$Projects
    )

    # Keywords for Kafka
    $MqKeywords = @{
        Packages = @(
            "Confluent.Kafka",
            "KafkaNETClient",
            "rdkafka-dotnet"
        )
        ConnKeys = @(
            "bootstrapservers", "bootstrap_servers", "servers", "brokerlist" # Common Kafka connection keys
        )
    }

    Debug-Log "Analyzing Kafka usage in projects..."

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

        # Detect Kafka libraries from packages
        foreach ($pkg in $pkgRefs) {
            foreach ($keyword in $MqKeywords.Packages) {
                if ($pkg -match [regex]::Escape($keyword)) {
                    $facts += @{Type="MQType"; Value="Kafka"}
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

            # Check root-level properties for Kafka settings
            foreach ($prop in $json.PSObject.Properties) {
                if ($prop.Name -match "Kafka") {
                    $value = $prop.Value
                    if ($null -ne $value -and ($value -is [string])) {
                        $facts += @{Type="KafkaConfig"; Value=$value}
                    }
                    elseif ($value -is [psobject]) {
                        foreach ($key in $MqKeywords.ConnKeys) {
                            if ($value.PSObject.Properties.Name -contains $key) {
                                $facts += @{Type="KafkaBootstrapServers"; Value=$value.$key}
                            }
                        }
                    }
                }
            }

            # Check ConnectionStrings section for Kafka connection strings
            if ($json.ConnectionStrings) {
                foreach ($connName in $json.ConnectionStrings.PSObject.Properties.Name) {
                    if ($connName -match "Kafka") {
                        $connString = $json.ConnectionStrings.$connName
                        $facts += @{Type="KafkaConnectionString"; Value=$connString}
                    }
                }
            }
        }
    }

    if ($facts.Count -eq 0) {
        Debug-Log "No Kafka facts found."
        return $null
    }

    return [PSCustomObject]@{
        AnalyzerName = "MessageQueue"
        Facts = $facts
    }
}
