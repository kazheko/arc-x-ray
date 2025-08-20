using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using System.Text.RegularExpressions;

namespace ProjectTypeDetection.Executors
{
    /// <summary>
    /// Searches for specific patterns or text content within project files.
    /// Supports wildcards, regex patterns, and recursive file searching.
    /// </summary>
    public class FileContentExecutor : ICheckExecutor
    {
        private readonly ILogger _logger;
        private readonly IFileRepository _fileRepository;

        public FileContentExecutor(ILogger logger, IFileRepository fileRepository)
        {
            _logger = logger;
            _fileRepository = fileRepository;
        }

        public string CheckType => "FileContent";

        /// <summary>
        /// Executes content search within project files.
        /// Searches for patterns or exact text in files specified by the target path.
        /// </summary>
        /// <param name="check">Check configuration with target files and pattern/expected value</param>
        /// <param name="projectContext">Project context with file information</param>
        /// <returns>True if pattern/value is found in any target file, false otherwise</returns>
        public async Task<bool> ExecuteAsync(Check check, ProjectContext projectContext)
        {
            try
            {
                // Step 1: Determine which files to search in
                var filesToSearch = GetTargetFiles(check.Target, projectContext);

                if (!filesToSearch.Any())
                {
                    // No files match the target pattern
                    // This is not necessarily an error - the files might not exist
                    return false;
                }

                // Step 2: Determine what to search for (pattern or exact text)
                var searchStrategy = DetermineSearchStrategy(check);

                if (searchStrategy == null)
                {
                    // No pattern or expected value specified - nothing to search for
                    return false;
                }

                // Step 3: Search through each file for the pattern/text
                foreach (var filePath in filesToSearch)
                {
                    bool foundInFile = await SearchInFileAsync(filePath, searchStrategy);

                    if (foundInFile)
                    {
                        // Found the pattern/text in at least one file
                        // No need to check other files
                        return true;
                    }
                }

                // Pattern/text not found in any of the target files
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Class: {nameof(NuGetPackageExecutor)}; Check: {check.Id}; Project: {projectContext.ProjectName}; Message: {ex.Message}.");

                return false;
            }
        }

        /// <summary>
        /// Gets the list of files to search based on the target pattern.
        /// Handles wildcards, recursive searches, and specific file paths.
        /// </summary>
        /// <param name="target">Target file pattern (e.g., "Program.cs", "*.json", "**/*.cs")</param>
        /// <param name="context">Project context with file lists</param>
        /// <returns>Collection of file paths to search</returns>
        private IEnumerable<string> GetTargetFiles(string target, ProjectContext context)
        {
            var matchingFiles = new List<string>();

            // Case 1: Recursive search pattern (starts with **)
            // Example: "**/*.cs" means all .cs files in any directory
            if (target.StartsWith("**"))
            {
                // Extract the file pattern after **/
                var filePattern = target.Substring(2).TrimStart('/', '\\');

                if (string.IsNullOrEmpty(filePattern))
                {
                    // "**" alone means all files
                    return context.AllFiles;
                }

                // Convert wildcard to regex for matching
                var regex = CreateFilePatternRegex(filePattern);

                // Search all files recursively
                matchingFiles.AddRange(
                    context.AllFiles.Where(file =>
                    {
                        var fileName = Path.GetFileName(file);
                        return regex.IsMatch(fileName);
                    })
                );
            }
            // Case 2: Wildcard in specific directory
            // Example: "Controllers/*.cs" means all .cs files in Controllers directory
            else if (target.Contains("*") || target.Contains("?"))
            {
                // Split into directory and file pattern
                var directory = Path.GetDirectoryName(target)?.Replace('\\', '/') ?? "";
                var filePattern = Path.GetFileName(target);

                // Build full directory path
                var fullDirectory = string.IsNullOrEmpty(directory)
                    ? context.ProjectPath
                    : Path.Combine(context.ProjectPath, directory);

                // Normalize directory path for comparison
                var normalizedDirectory = NormalizePath(fullDirectory);

                // Create regex for file pattern
                var regex = CreateFilePatternRegex(filePattern);

                // Find files in the specific directory that match the pattern
                matchingFiles.AddRange(
                    context.AllFiles.Where(file =>
                    {
                        var fileDir = Path.GetDirectoryName(file);
                        var fileName = Path.GetFileName(file);

                        // Check if file is in the target directory
                        if (fileDir == null || !NormalizePath(fileDir).Equals(normalizedDirectory, StringComparison.OrdinalIgnoreCase))
                            return false;

                        // Check if filename matches the pattern
                        return regex.IsMatch(fileName);
                    })
                );
            }
            // Case 3: Specific file path (no wildcards)
            // Example: "Program.cs" or "Controllers/WeatherController.cs"
            else
            {
                // Build the full path for the specific file
                var fullPath = Path.IsPathRooted(target)
                    ? target
                    : Path.Combine(context.ProjectPath, target);

                // Normalize for comparison
                var normalizedTarget = NormalizePath(fullPath);

                // Find the exact file in the context
                var exactMatch = context.AllFiles.FirstOrDefault(file =>
                    NormalizePath(file).Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(exactMatch))
                {
                    matchingFiles.Add(exactMatch);
                }
            }

            return matchingFiles;
        }

        /// <summary>
        /// Determines what search strategy to use based on check configuration.
        /// Returns a function that can test if content matches the criteria.
        /// </summary>
        /// <param name="check">Check configuration with pattern or expected value</param>
        /// <returns>Search function, or null if no search criteria specified</returns>
        private Func<string, bool>? DetermineSearchStrategy(Check check)
        {
            // Priority 1: Regex pattern search
            // Used for complex pattern matching
            // Example: Pattern = @"WebApplication\.CreateBuilder\(args\)"
            if (!string.IsNullOrEmpty(check.Pattern))
            {
                // Compile regex with multiline option for searching across lines
                var regex = new Regex(check.Pattern,
                    RegexOptions.Multiline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

                // Return a function that tests if content matches the regex
                return regex.IsMatch;
            }

            // Priority 2: Exact text search
            // Used for simple string containment
            // Example: ExpectedValue = "AddControllers()"
            if (!string.IsNullOrEmpty(check.ExpectedValue))
            {
                // Return a function that checks if content contains the expected text
                // Case-insensitive search for flexibility
                return content => content.IndexOf(check.ExpectedValue, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            // No search criteria specified
            return null;
        }

        /// <summary>
        /// Searches for content within a specific file.
        /// Reads the file and applies the search strategy.
        /// </summary>
        /// <param name="filePath">Full path to the file to search</param>
        /// <param name="searchStrategy">Function that tests if content matches criteria</param>
        /// <returns>True if content is found, false otherwise</returns>
        private async Task<bool> SearchInFileAsync(string filePath, Func<string, bool> searchStrategy)
        {
            // Read the entire file content
            var content = await _fileRepository.ReadFileAsync(filePath);

            if (string.IsNullOrEmpty(content))
            {
                // For large files, the content will be empty as well
                return false;
            }

            // Apply the search strategy to the content
            return searchStrategy(content);
        }

        /// <summary>
        /// Creates a regex pattern from a file wildcard pattern.
        /// Converts wildcards (* and ?) to regex equivalents.
        /// </summary>
        /// <param name="wildcardPattern">Wildcard pattern (e.g., "*.cs", "Test?.txt")</param>
        /// <returns>Compiled regex for matching filenames</returns>
        private Regex CreateFilePatternRegex(string wildcardPattern)
        {
            // Escape special regex characters except * and ?
            var pattern = Regex.Escape(wildcardPattern);

            // Convert wildcards to regex
            // * = zero or more characters
            // ? = exactly one character
            pattern = pattern.Replace("\\*", ".*");
            pattern = pattern.Replace("\\?", ".");

            // Anchor the pattern to match the entire filename
            pattern = "^" + pattern + "$";

            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// Normalizes file paths for consistent comparison.
        /// Handles different path separators and case sensitivity.
        /// </summary>
        /// <param name="path">Path to normalize</param>
        /// <returns>Normalized path string</returns>
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            // Replace all separators with the system separator
            path = path.Replace('/', Path.DirectorySeparatorChar);
            path = path.Replace('\\', Path.DirectorySeparatorChar);

            // Remove trailing separator
            path = path.TrimEnd(Path.DirectorySeparatorChar);

            // On Windows, normalize to lowercase for case-insensitive comparison
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                path = path.ToLowerInvariant();
            }

            return path;
        }
    }
}