using ArcXray.Analyzers.Applications.Checks;
using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;

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

        public async Task<DetectionResult> AnalyzeProjectAsync(ProjectContext projectContext, DetectionConfiguration config)
        {
            var startTime = DateTime.UtcNow;
            var result = new DetectionResult
            {
                ProjectPath = projectContext.ProjectPath,
                ProjectType = config.Metadata.ProjectType,
                AnalysisTimestamp = startTime
            };

            // Execute all checks
            foreach (var check in config.Checks)
            {
                var checkResult = await ExecuteCheckAsync(check, projectContext);
                result.Checks.Add(checkResult);
            }

            // Determine interpretation
            DetermineInterpretation(result, config);

            result.AnalysisDuration = DateTime.UtcNow - startTime;
            return result;
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

        private void DetermineInterpretation(DetectionResult result, DetectionConfiguration config)
        {
            var threshold = config.InterpretationRules.Thresholds
                .FirstOrDefault(t => result.Confidence >= t.Min && result.Confidence <= t.Max);

            if (threshold != null)
            {
                result.Interpretation = threshold.Interpretation;
                result.ConfidenceLevel = threshold.Confidence;
            }
            else
            {
                result.Interpretation = "Unable to determine";
                result.ConfidenceLevel = "unknown";
            }

            // Check minimum requirements for high confidence
            if (result.ConfidenceLevel == "high" &&
                config.InterpretationRules.MinimumChecksForConfidence.TryGetValue("high", out var requiredChecks))
            {
                var missingChecks = requiredChecks.Where(checkId =>
                    !result.Checks.Any(c => c.CheckId == checkId && c.Passed)).ToList();

                if (missingChecks.Any())
                {
                    result.ConfidenceLevel = "medium-high";
                    result.Interpretation = $"{result.Interpretation} (missing critical checks: {string.Join(", ", missingChecks)})";
                }
            }
        }
    }
}
