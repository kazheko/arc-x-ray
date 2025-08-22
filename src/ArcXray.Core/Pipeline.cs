using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;

namespace ArcXray.Core
{
    public class Pipeline
    {
        private readonly ILogger _logger;
        private readonly IAnalyzeRepository _structureAnalyzer;
        private readonly IAnalyzeApplication _analyzeApplication;
        private readonly IBuildProjectContext _buildProjectContext;
        private readonly IProvideCheckList _provideDetectionConfig;

        public Pipeline
        (
            IAnalyzeRepository structureAnalyzer,
            IAnalyzeApplication analyzeApplication,
            IBuildProjectContext buildProjectContext,
            IProvideCheckList provideDetectionConfig,
            ILogger logger)
        {
            _structureAnalyzer = structureAnalyzer;
            _analyzeApplication = analyzeApplication;
            _buildProjectContext = buildProjectContext;
            _provideDetectionConfig = provideDetectionConfig;
            _logger = logger;
        }

        public async Task ExecuteAsync(string repoPath, string[] excludeKeywords, string detectionConfigPath)
        {
            _logger.Debug($"Starting analysis for repository: {repoPath}");

            var structure = await _structureAnalyzer.AnalyzeAsync(repoPath, excludeKeywords);

            _logger.Debug($"Repository structure analyzed. Found {structure.Solutions.Count()} solution(s).");

            foreach (var solution in structure.Solutions)
            {
                _logger.Debug($"Processing solution: {solution.Name}, projects count: {solution.ProjectPaths.Count()}");

                foreach (var projectPath in solution.ProjectPaths)
                {
                    _logger.Debug($"Analyzing project at path: {projectPath}");

                    var context = _buildProjectContext.CreateFromCsproj(projectPath);

                    var framework = context.TargetFrameworks.First(); // todo: rework
                    _logger.Debug($"Detected target framework: {framework}, SDK: {context.Sdk}");

                    var configs = await _provideDetectionConfig.GetConfigAsync(framework, context.Sdk, detectionConfigPath);
                    _logger.Debug($"Loaded {configs.Count()} detection configuration(s) for the project.");

                    foreach (var config in configs)
                    {
                        _logger.Debug($"Running analysis with configuration: {config.Metadata.ProjectType}");

                        var result = await _analyzeApplication.AnalyzeProjectAsync(context, config);

                        _logger.Info($"=== {result.ProjectType} Detection Results ===");
                        _logger.Info($"Project: {result.ProjectPath}");
                        _logger.Info($"Confidence: {result.Confidence:P2}");
                        _logger.Info($"Interpretation: {result.Interpretation}");
                    }
                }
            }

            _logger.Debug($"Analysis completed");
        }
    }
}
