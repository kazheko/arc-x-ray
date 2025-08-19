using ArcXray.Analyzers.Applications.Models;
using ArcXray.Contracts;
using ArcXray.Contracts.Application;

namespace ArcXray.Analyzers.Applications
{
    public class ApplicationAnalyzer
    {
        private readonly DetectionConfiguration _config;
        private readonly Dictionary<string, ICheckExecutor> _executors;

        public ApplicationAnalyzer(DetectionConfiguration config, Dictionary<string, ICheckExecutor> executors)
        {
            _config = config;
            _executors = executors;
        }

        public async Task<DetectionResult> AnalyzeProjectAsync(ProjectContext projectContext)
        {
            var startTime = DateTime.UtcNow;
            var result = new DetectionResult
            {
                ProjectPath = projectContext.ProjectPath,
                ProjectType = _config.Metadata.ProjectType,
                AnalysisTimestamp = startTime
            };

            // Execute all checks
            foreach (var check in _config.Checks)
            {
                var checkResult = await ExecuteCheckAsync(check, projectPath);
                result.Checks.Add(checkResult);
            }

            // Determine interpretation
            DetermineInterpretation(result);

            result.AnalysisDuration = DateTime.UtcNow - startTime;
            return result;
        }

        private async Task<CheckResult> ExecuteCheckAsync(Check check, string projectPath)
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
                    checkResult.Passed = await executor.ExecuteAsync(check, projectPath);
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

        private void DetermineInterpretation(DetectionResult result)
        {
            var threshold = _config.InterpretationRules.Thresholds
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
                _config.InterpretationRules.MinimumChecksForConfidence.TryGetValue("high", out var requiredChecks))
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
