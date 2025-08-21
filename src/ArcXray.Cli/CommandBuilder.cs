using System.CommandLine;

namespace ArcXray.Cli
{
    internal static class CommandBuilder
    {
        public static RootCommand Build(Func<string, string[], string, Task> actionAsync)
        {
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
            rootCommand.SetAction(async result =>
            {
                var repoPath = result.GetValue(repoPathOption);
                var exclude = result.GetValue(excludeOption);
                var report = result.GetValue(reportOption);

                if (!Directory.Exists(repoPath))
                {
                    Console.WriteLine("Invalid path: " + repoPath);
                    return;
                }

                var excludeKeywords = exclude.Split(';', StringSplitOptions.RemoveEmptyEntries);

                // Setup services

                // todo: remove hard-coded strings
                await actionAsync(repoPath, excludeKeywords, "./knowledge-base/app-detection/");
            });

            return rootCommand;
        }
    }
}
