# .NET Web Service Analyzer

A PowerShell-based tool for analyzing .NET repositories with multiple ASP.NET Core web services.  
Each web service is considered a **group of projects**, where the root is an ASP.NET Core web project (`.csproj`)  
and child projects are included via `<ProjectReference>` links.

The current implementation focuses on detecting **database usage**  
(e.g., DB type, libraries, server, and database name).  
The architecture allows adding new analyzers easily.

---

## 📂 Project Structure

```
arcxray/
│
├── main.ps1 # Main orchestrator script
│
├── lib/
│ ├── Find-WebProjects.ps1 # Finds ASP.NET Core web projects and their child projects
│ ├── Load-Analyzers.ps1 # Loads analyzer scripts
│ ├── Report-Generator.ps1 # Generates the final analysis report
│ ├── Utils.ps1 # Common utility functions
│
└── analyzers/
├── Analyzer.Database.ps1 # Analyzer for database usage detection
└── Analyzer.MessageBus.ps1# (Example placeholder for message bus analyzer)
```

---

## 🚀 Features

- **Auto-detect ASP.NET Core web projects** based on `Microsoft.NET.Sdk.Web` or `Microsoft.AspNetCore.App` reference.
- **Recursive dependency resolution** — finds all child projects via `<ProjectReference>` tags.
- **Pluggable analyzers** — add new `.ps1` files to `analyzers/` with a common return format.
- **Database usage detection** — finds:
  - DB library (EF Core, Dapper, Npgsql, etc.)
  - DB server name (if found in connection strings)
  - DB name (if found in connection strings)
- **Exclusion filter** — skip projects with certain keywords in their paths.

---

## 📦 Requirements

- **Windows PowerShell 5.1** or **PowerShell Core 7+**
- Access to the `.NET` repository you want to analyze

---

## 🔧 Usage

Run from PowerShell:

```powershell
# Basic usage
.\main.ps1 -RepoPath "C:\Path\To\Repository"

# Exclude test projects
.\main.ps1 -RepoPath "C:\Path\To\Repository" -ExcludeKeywords "Test","Mock"

```

## 📝 Example Output

Starting analysis on repository: C:\MyRepo
Found 2 web project groups.
Loaded 1 analyzers.
Analyzing web service rooted at: C:\MyRepo\WebApp1\WebApp1.csproj
  Running analyzer: Database
  Found facts:
    DBLibrary: EntityFrameworkCore
    DBServer: sqlserver01
    DBName: MyDatabase

Analyzing web service rooted at: C:\MyRepo\WebApp2\WebApp2.csproj
  Running analyzer: Database
  Found facts:
    DBLibrary: Npgsql (PostgreSQL)
    DBServer: pg-server
    DBName: OrdersDB

## 📂 Adding New Analyzers

1. Create a new file in analyzers/ named Analyzer.<Name>.ps1.

2. Define a function named Invoke-Analyzer-<Name>:

```
function Invoke-Analyzer-MyAnalyzer {
    param ([string[]]$Projects)
    $facts = @(
        @{Type="MyFactType"; Value="MyFactValue"}
    )
    return [PSCustomObject]@{
        AnalyzerName = "MyAnalyzer"
        Facts = $facts
    }
}

```

3. The analyzer must return a [PSCustomObject] with:
    - AnalyzerName — unique analyzer name
    - Facts — array of @{Type=""; Value=""} hashtables

4. The script will load and run it automatically.