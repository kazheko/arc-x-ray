using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;

namespace ArcXray.Cli.Loggers
{
    internal class AppAnalysisLogger : IAnalyzeApplication
    {
        private readonly DateTime _dateTime = DateTime.UtcNow;

        private readonly IAnalyzeApplication _analyzer;

        public AppAnalysisLogger(IAnalyzeApplication analyzer)
        {
            _analyzer = analyzer;
        }

        public async Task<DetectionResult> AnalyzeProjectAsync(ProjectContext projectContext, DetectionConfiguration config)
        {
            var result = await _analyzer.AnalyzeProjectAsync(projectContext, config);

            var datetime = _dateTime.ToString("dd-MM-yy-HH-mm-ss");

            var filename = $"Debug-Logs/{datetime}/{result.ProjectName}/{config.Metadata.ProjectType}.txt";

            var dir = Path.GetDirectoryName(filename);

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // Display results
            await File.AppendAllTextAsync(filename, $"=== Web API Detection Results ===" + Environment.NewLine);
            await File.AppendAllTextAsync(filename, $"Project: {result.ProjectPath}" + Environment.NewLine);
            await File.AppendAllTextAsync(filename, $"Confidence: {result.Confidence:P2}" + Environment.NewLine);
            await File.AppendAllTextAsync(filename, $"Interpretation: {result.Interpretation}" + Environment.NewLine);
            await File.AppendAllTextAsync(filename, Environment.NewLine);

            // Show detailed check results
            var groupedChecks = result.Checks
                .GroupBy(c => c.Category);

            foreach (var group in groupedChecks)
            {
                await File.AppendAllTextAsync(filename, $"\n[{group.Key}]" + Environment.NewLine);
                foreach (var check in group.OrderByDescending(c => c.Weight))
                {
                    var status = check.Passed ? "✅" : "❌";
                    await File.AppendAllTextAsync(filename, $"  {status} {check.Description} (weight: {check.Weight:F1})" + Environment.NewLine);
                    if (!check.Passed && !string.IsNullOrEmpty(check.Details))
                    {
                        await File.AppendAllTextAsync(filename, $"     → {check.Details}" + Environment.NewLine);
                    }
                }
            }

            return result;
        }
    }
}
