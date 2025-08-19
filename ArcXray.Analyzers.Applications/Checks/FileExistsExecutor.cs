using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.FileSystemGlobbing;

namespace ProjectTypeDetection.Executors
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
                _logger.Error($"Class: {nameof(NuGetPackageExecutor)}; Check: {check.Id}; Project: {projectContext.ProjectName}; Message: {ex.Message}.");

                // Return false for any exceptions (e.g., access denied, invalid path)
                return Task.FromResult(false);
            }
        }

        private bool IsFileExist(string path, ProjectContext projectContext)
        {
            var normalizedTarget = NormalizePath(path);
            return projectContext.AllFiles
                .Any(file => NormalizePath(file).Equals(normalizedTarget));
        }

        private bool IsDirectoryExist(string directoryPath, ProjectContext projectContext)
        {
            var normalizedDir = NormalizePath(directoryPath);

            return projectContext.AllFiles
                .Any(file =>Inside(file, normalizedDir));
        }

        private bool Inside(string file, string normalizedDir)
        {
            var fileDir = Path.GetDirectoryName(file);
            return fileDir != null &&
                   (NormalizePath(fileDir).Equals(normalizedDir) ||
                    NormalizePath(fileDir).StartsWith(normalizedDir + Path.DirectorySeparatorChar));
        }

        private IEnumerable<string> GetFiles(string pattern, ProjectContext projectContext)
        {
            var matcher = new Matcher();
            matcher.AddInclude(pattern);

            return projectContext.AllFiles
                .Where(file => matcher.Match(projectContext.ProjectPath, file).HasMatches);
        }

        /// <summary>
        /// Normalizes file paths for consistent comparison.
        /// Handles different path separators and case sensitivity.
        /// </summary>
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // Replace alternative separators with standard ones
            path = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

            // Remove trailing separators
            path = path.TrimEnd(Path.DirectorySeparatorChar);

            // Convert to lowercase for case-insensitive comparison on Windows
            // Keep original case on Unix-like systems
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                path = path.ToLowerInvariant();
            }

            return path;
        }
    }
}