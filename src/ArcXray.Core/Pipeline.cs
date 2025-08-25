using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.Reporting;
using ArcXray.Contracts.RepositoryStructure;
using ArcXray.Core.Steps;

namespace ArcXray.Core
{
    public class Pipeline
    {
        private readonly ILogger _logger;
        private readonly IAnalyzeRepository _structureAnalyzer;
        private readonly IAnalyzeApplication _analyzeApplication;
        private readonly IBuildProjectContext _buildProjectContext;
        private readonly IProvideCheckList _provideDetectionConfig;
        private readonly IGenerateReport _reportGenerator;

        public Pipeline
        (
            IAnalyzeRepository structureAnalyzer,
            IAnalyzeApplication analyzeApplication,
            IBuildProjectContext buildProjectContext,
            IProvideCheckList provideDetectionConfig,
            ILogger logger,
            IGenerateDiagram projectDependencyGraphGenerator,
            IGenerateReport reportGenerator
        )
        {
            _structureAnalyzer = structureAnalyzer;
            _analyzeApplication = analyzeApplication;
            _buildProjectContext = buildProjectContext;
            _provideDetectionConfig = provideDetectionConfig;
            _logger = logger;
            _reportGenerator = reportGenerator;
        }

        public async Task ExecuteAsync(string repoPath, string[] excludeKeywords, string detectionConfigPath)
        {
            _logger.Debug($"Starting analysis for repository: {repoPath}");

            var structure = await _structureAnalyzer.AnalyzeAsync(repoPath, excludeKeywords);
            var path1 = await _reportGenerator.AppendRepositoryInfoAsync(structure);

            _logger.Debug($"Repository structure analyzed. Found {structure.Solutions.Count()} solution(s).");

            foreach (var solution in structure.Solutions)
            {
                _logger.Debug($"Processing solution: {solution.Name}, projects count: {solution.AllProjects.Count()}");

                var step1 = new AnalyzeEntryPoints(_logger, _buildProjectContext, _provideDetectionConfig, _analyzeApplication);
                var entryPointResults = await step1.AnalyzeAsync(solution, detectionConfigPath);

                var path2 = await _reportGenerator.AppendSolutionInfoAsync(solution);
            }

            _logger.Debug($"Analysis completed");
        }
    }
}
