using ArcXray.Cli;
using ArcXray.Core.RepositoryStructure;
using System.CommandLine;

// Define command-line options
var repoPathOption = new Option<string>("--repo-path")
{ 
    Description = "Path to the repository root",
    Required = true,
};

var excludeOption = new Option<string>("--exclude")
{
    Description = "Semicolon-separated list of keywords to exclude projects",
    DefaultValueFactory = (a) => ""
};

var reportOption = new Option<string>("--report")
{
    Description = "Report strategy: console, json, etc.",
    DefaultValueFactory = a => "console"
};

// Root command
var rootCommand = new RootCommand("Arc X-ray - Analyze .NET solutions and projects")
{
    repoPathOption,
    excludeOption,
    reportOption
};

// Command handler
rootCommand.SetAction(result =>
{
    var repoPath = result.GetValue(repoPathOption);
    var exclude = result.GetValue(excludeOption);
    var report = result.GetValue(reportOption);

    if (!Directory.Exists(repoPath))
    {
        Console.WriteLine("❌ Invalid path: " + repoPath);
        return;
    }

    var excludeKeywords = exclude.Split(';', StringSplitOptions.RemoveEmptyEntries);

    // Setup services
    var repositoryInfo = RepositoryAnalyzer.Analyze(repoPath, excludeKeywords);

    // Choose report strategy
    IReportStrategy reportStrategy = report.ToLower() switch
    {
        "console" => new ConsoleReportStrategy(),
        _ => new ConsoleReportStrategy()
    };

    // Generate report
    reportStrategy.GenerateReport(repositoryInfo);

});

ParseResult parseResult = rootCommand.Parse(args);
return await parseResult.InvokeAsync();
