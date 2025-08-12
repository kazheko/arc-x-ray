param(
    [Parameter(Mandatory = $true)]
    [string]$Root,

    [string[]]$ExcludeKeywords = @()
)

# Find all .csproj files recursively, exclude projects by keywords if provided
function Get-WebProjects {
    param([string]$Path, [string[]]$Excludes)

    $projects = Get-ChildItem -Path $Path -Recurse -Filter *.csproj -ErrorAction SilentlyContinue

    if ($Excludes.Count -gt 0) {
        # Build regex pattern to exclude projects by keywords in full path or name
        $pattern = ($Excludes | ForEach-Object {[regex]::Escape($_)}) -join "|"
        $projects = $projects | Where-Object { $_.FullName -notmatch $pattern }
    }

    return $projects
}

# Determine if the project is an ASP.NET Core Web project by SDK and code indicators
function Is-AspNetCoreProject {
    param([string]$ProjectFile)

    $content = Get-Content -Path $ProjectFile -Raw -ErrorAction SilentlyContinue
    if ($null -eq $content) { return $false }

    # 1. Check if the project uses the Web SDK
    if ($content -match 'Sdk\s*=\s*"Microsoft\.NET\.Sdk\.Web"') {
        return $true
    }

    # 2. Check for core ASP.NET Core web packages
    if ($content -match 'Microsoft\.AspNetCore\.App' -or
        $content -match 'Microsoft\.AspNetCore\.Hosting' -or
        $content -match 'Microsoft\.AspNetCore\.Server\.Kestrel') {

        # 3. Verify web host entry points in Program.cs or Startup.cs
        $projDir = Split-Path $ProjectFile -Parent
        $programFiles = Get-ChildItem $projDir -Recurse -Filter Program.cs -ErrorAction SilentlyContinue | 
                        Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }
        $startupFiles = Get-ChildItem $projDir -Recurse -Filter Startup.cs -ErrorAction SilentlyContinue |
                        Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }

        $filesToCheck = @()
        if ($programFiles.Count -gt 0) { $filesToCheck += $programFiles }
        if ($startupFiles.Count -gt 0) { $filesToCheck += $startupFiles }

        foreach ($file in $filesToCheck) {
            if (-not (Test-Path $file.FullName)) { continue }
            try {
                $code = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
                if ($code -match 'WebApplication\.CreateBuilder' -or
                    $code -match 'ConfigureWebHostDefaults' -or
                    $code -match 'WebHost\.CreateDefaultBuilder') {
                    return $true
                }
            }
            catch {
                continue
            }
        }
    }

    return $false
}

# Extract TargetFramework from the csproj file
function Get-TargetFramework {
    param([string]$CsprojPath)
    $content = Get-Content -Path $CsprojPath -Raw -ErrorAction SilentlyContinue
    if ($null -ne $content -and $content -match "<TargetFramework>(.*?)</TargetFramework>") {
        return $matches[1]
    }
    return ""
}

# Detect project purpose (API, MVC, RazorPages, MinimalAPI) and roles (Gateway, IdP)
function Detect-WebPurposeAndRole {
    param([string]$ProjectDir)

    # Define regex patterns for different web purposes
    $purposePatterns = @{
        API          = @("AddControllers\(", "MapControllers\(")
        MVC          = @("AddControllersWithViews\(|MapControllerRoute\(|AddMvc\(")
        RazorPages   = @("AddRazorPages\(|MapRazorPages\(")
        MinimalAPI   = @("MapGet\(", "MapPost\(", "MapPut\(", "MapDelete\(", "MapHealthChecks\(", "UseEndpoints\(")
    }

    # Define patterns for specific roles
    $rolePatterns = @{
        "ApiGateway(Ocelot)"    = "UseOcelot\("
        "ApiGateway(Yarp)"      = "AddReverseProxy\("
        "IdentityServer"        = "UseIdentityServer\("
        "SignalRHub"            = "AddSignalR|MapHub"
        "gRPCService"           = "AddGrpc|MapGrpcService"
    }

    $purpose = @()
    $roles = @()

    # Get all .cs files excluding bin and obj folders
    $allCsFiles = Get-ChildItem -Path $ProjectDir -Recurse -Filter *.cs -ErrorAction SilentlyContinue |
                  Where-Object { $_.FullName -notmatch "\\bin\\|\\obj\\" }

    foreach ($file in $allCsFiles) {
        try {
            $code = Get-Content -Path $file.FullName -Raw -ErrorAction Stop
        } catch {
            continue
        }

        # Normalize code to a single line for simpler regex matching
        $codeCompact = $code -replace "\r?\n", " " -replace "\s{2,}", " "

        # Check for web purposes
        foreach ($key in $purposePatterns.Keys) {
            foreach ($pattern in $purposePatterns[$key]) {
                if ($codeCompact -match $pattern) {
                    $purpose += $key
                    break
                }
            }
        }

        # Check for roles
        foreach ($role in $rolePatterns.Keys) {
            if ($code -match $rolePatterns[$role]) {
                $roles += $role
            }
        }
    }

    # Remove duplicates and join results with commas
    $uniquePurposes = $purpose | Sort-Object -Unique
    $uniqueRoles = $roles | Sort-Object -Unique

    $combined = @()
    if ($uniqueRoles.Count -gt 0) { $combined += $uniqueRoles }
    if ($uniquePurposes.Count -gt 0) { $combined += $uniquePurposes }

    $combinedStr = $combined -join ", "

    return [PSCustomObject]@{
        Combined = $combinedStr
        Roles = $uniqueRoles -join ", "
        Purpose = $uniquePurposes -join ", "
    }
}

# Main analysis function to get projects and their analysis info
function Analyze-WebProjects {
    param([string]$Path, [string[]]$Excludes)

    $projects = Get-WebProjects -Path $Path -Excludes $Excludes
    $results = @()

    foreach ($proj in $projects) {
        if (Is-AspNetCoreProject -ProjectFile $proj.FullName) {
            $framework = Get-TargetFramework -CsprojPath $proj.FullName
            $projDir   = Split-Path $proj.FullName -Parent
            $analysis  = Detect-WebPurposeAndRole -ProjectDir $projDir

            $results += [PSCustomObject]@{
                Project         = $proj.BaseName  # Filename without extension
                TargetFramework = $framework
                RolesAndPurpose = $analysis.Combined
            }
        }
    }

    return $results
}

# Run the analysis and output results
$analysis = Analyze-WebProjects -Path $Root -Excludes $ExcludeKeywords

if ($null -eq $analysis -or $analysis.Count -eq 0) {
    Write-Host "No ASP.NET Core projects detected under $Root" -ForegroundColor Yellow
} else {
    $analysis | Format-Table -AutoSize
}
