using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace ArcXray.Analyzers.Applications.Checks
{
    /// <summary>
    /// Checks if a file or directory exists in the project structure.
    /// Supports wildcards and alternative paths.
    /// </summary>
    public class FileExistsExecutor : ICheckExecutor
    {
        private readonly ILogger _logger;

        public FileExistsExecutor(ILogger logger)
        {
            _logger = logger;
        }

        public string CheckType => "FileExists";

        public Task<bool> ExecuteAsync(Check check, ProjectContext projectContext)
        {
            var projectPath = projectContext.ProjectPath;

            try
            {
                // Build the full path by combining project path with target
                // Example: "C:\Projects\MyApi" + "Controllers/" = "C:\Projects\MyApi\Controllers\"
                var targetPath = Path.Combine(projectPath, check.Target);

                // Check if we're looking for a directory (ends with / or \)
                // Example: "Controllers/" means we're checking for a directory
                if (check.Target.EndsWith("/") || check.Target.EndsWith("\\"))
                {
                    if (IsDirectoryExist(targetPath, projectContext))
                        return Task.FromResult(true);
                }
                else
                {
                    // Handle file checks with potential wildcards
                    // Example: "Controllers/*.cs" means any .cs file in Controllers folder
                    if (check.Target.Contains("*"))
                    {
                        // Search for files matching the pattern
                        var files = GetFiles(check.Target, projectContext);
                        if (files.Any())
                            return Task.FromResult(true);
                        
                    }
                    else if (IsFileExist(targetPath, projectContext))
                    {
                        // Simple file existence check without wildcards
                        // Example: "Program.cs" - check if this specific file exists
                        return Task.FromResult(true);
                    }
                }

                // If main target not found, check alternative targets
                // Example: check.AlternativeTargets = ["DTOs/", "Contracts/"]
                // This is useful when projects might use different naming conventions
                if (check.AlternativeTargets != null)
                {
                    foreach (var altTarget in check.AlternativeTargets)
                    {
                        var altPath = Path.Combine(projectPath, altTarget);
                        if (IsDirectoryExist(altPath, projectContext) || IsFileExist(altPath, projectContext))
                            return Task.FromResult(true);
                    }
                }

                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"Class: {nameof(FileExistsExecutor)}; Check: {check.Id}; Project: {projectContext.ProjectName}; Message: {ex.Message}.");

                // Return false for any exceptions (e.g., access denied, invalid path)
                return Task.FromResult(false);
            }
        }

        private bool IsFileExist(string path, ProjectContext projectContext)
        {
            var normalizedTarget = Helpers.NormalizePath(path);
            return projectContext.AllFiles
                .Any(file => Helpers.NormalizePath(file).Equals(normalizedTarget));
        }

        private bool IsDirectoryExist(string directoryPath, ProjectContext projectContext)
        {
            var normalizedDir = Helpers.NormalizePath(directoryPath);

            return projectContext.AllFiles
                .Any(file =>Inside(file, normalizedDir));
        }

        private bool Inside(string file, string normalizedDir)
        {
            var fileDir = Path.GetDirectoryName(file);
            return fileDir != null &&
                   (Helpers.NormalizePath(fileDir).Equals(normalizedDir) ||
                    Helpers.NormalizePath(fileDir).StartsWith(normalizedDir + Path.DirectorySeparatorChar));
        }

        private IEnumerable<string> GetFiles(string pattern, ProjectContext projectContext)
        {
            var directory = Path.GetDirectoryName(pattern) ?? "";
            var filePattern = Path.GetFileName(pattern);
            var rexexPattern = Helpers.WildcardToRegex(filePattern);
            var regex = new Regex(rexexPattern, RegexOptions.IgnoreCase);

            var fullDir = Path.Combine(projectContext.ProjectPath, directory);

            return projectContext.AllFiles
                .Where(file =>
                {
                    var fileDir = Path.GetDirectoryName(file);
                    var fileName = Path.GetFileName(file);
                    return fileDir != null &&
                           Helpers.NormalizePath(fileDir).Equals(Helpers.NormalizePath(fullDir)) &&
                           regex.IsMatch(fileName);
                });
        }
    }
}