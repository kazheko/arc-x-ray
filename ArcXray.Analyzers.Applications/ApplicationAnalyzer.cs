using ArcXray.Analyzers.Applications.Checks;
using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;
using System.Text.RegularExpressions;

namespace ArcXray.Analyzers.Applications
{
    public class ApplicationAnalyzer : IAnalyzeApplication
    {
        private readonly ILogger _logger;
        private readonly Dictionary<string, ICheckExecutor> _executors;
        private readonly IFileRepository _fileRepository;

        public ApplicationAnalyzer(ILogger logger, IFileRepository fileRepository)
        {
            _logger = logger;
            _fileRepository = fileRepository;

            _executors = new Dictionary<string, ICheckExecutor>
            {
                { "FileExists", new FileExistsExecutor(logger) },
                { "ProjectFile", new ProjectFileExecutor(logger) },
                { "FileContent", new FileContentExecutor(logger, fileRepository) },
                { "NuGetPackage", new NuGetPackageExecutor(logger) },
                { "CodeAnalysis", new CodeAnalysisExecutor(logger, fileRepository) }
            };
            
        }

        public async Task<DetectionResult> AnalyzeProjectAsync(ProjectContext projectContext, CheckList config)
        {
            var startTime = DateTime.UtcNow;
            var result = new DetectionResult
            {
                ProjectName = projectContext.ProjectName,
                ProjectPath = projectContext.ProjectPath,
                ProjectType = config.Metadata.ProjectType,
                AnalysisTimestamp = startTime
            };

            var framework = projectContext.TargetFrameworks.First();
            var checks = config.Checks
                .Where(x => x.Frameworks.Select(CreateFilePatternRegex)
                .Any(regexp => regexp.IsMatch(framework)));

            foreach (var check in checks)
            {
                var checkResult = await ExecuteCheckAsync(check, projectContext);
                result.AddCheckResult(checkResult);
            }

            // Determine interpretation
            var interpretation = DetermineInterpretation(result, config);
            result.UpdateInterpretation(interpretation);

            result.AnalysisDuration = DateTime.UtcNow - startTime;
            return result;
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

        private async Task<CheckResult> ExecuteCheckAsync(Check check, ProjectContext projectContext)
        {
            var checkResult = new CheckResult
            {
                CheckId = check.Id,
                Weight = check.Weight,
                Category = check.Category,
                Description = check.Description
            };

            try
            {
                if (_executors.TryGetValue(check.Type, out var executor))
                {
                    checkResult.Passed = await executor.ExecuteAsync(check, projectContext);
                    checkResult.Details = checkResult.Passed ? "Check passed" : "Check failed";
                }
                else
                {
                    checkResult.Passed = false;
                    checkResult.Details = $"No executor found for check type: {check.Type}";
                }
            }
            catch (Exception ex)
            {
                checkResult.Passed = false;
                checkResult.Details = $"Error executing check: {ex.Message}";
            }

            return checkResult;
        }

        private string DetermineInterpretation(DetectionResult result, CheckList checkList)
        {
            switch (result.Confidence)
            {
                case double confidence when (confidence > 0.85 && confidence <= 1.0):
                    return $"Definitely {checkList.Metadata.ProjectType}";
                case double confidence when (confidence > 0.70 && confidence <= 0.85):
                    return $"Likely {checkList.Metadata.ProjectType} with non-standard configuration";
                case double confidence when (confidence > 0.50 && confidence <= 0.70):
                    return $"Partial {checkList.Metadata.ProjectType} usage";
                default:
                    return $"Likely NOT {checkList.Metadata.ProjectType}";
            }
        }
    }
}
