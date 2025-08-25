using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;

namespace ArcXray.Core.Steps
{
    internal class AnalyzeEntryPoints
    {
        private readonly ILogger _logger;
        private readonly IBuildProjectContext _buildProjectContext;
        private readonly IProvideCheckList _checkListsProvider;
        private readonly IAnalyzeApplication _analyzeApplication;

        public AnalyzeEntryPoints(ILogger logger, IBuildProjectContext buildProjectContext, IProvideCheckList checkListsProvider, IAnalyzeApplication analyzeApplication)
        {
            _logger = logger;
            _buildProjectContext = buildProjectContext;
            _checkListsProvider = checkListsProvider;
            _analyzeApplication = analyzeApplication;
        }

        public async Task<IEnumerable<DetectionResult>> AnalyzeAsync(SolutionInfo solutionInfo, string detectionConfigPath)
        {
            var results = new List<DetectionResult>();

            var projects = solutionInfo.RootProjects;

            foreach (var project in projects)
            {
                _logger.Debug($"Analyzing project at path: {project}");

                var projectContext = _buildProjectContext.BuildProjectContext(project);

                var framework = projectContext.TargetFrameworks.First(); // todo: rework
                _logger.Debug($"Detected target framework: {framework}, SDK: {projectContext.Sdk}");

                var configs = await _checkListsProvider.GetConfigAsync(framework, projectContext.Sdk, detectionConfigPath);
                _logger.Debug($"Loaded {configs.Count()} detection configuration(s) for the project.");

                foreach (var config in configs)
                {
                    _logger.Debug($"Running analysis with configuration: {config.Metadata.ProjectType}");

                    var result = await _analyzeApplication.AnalyzeProjectAsync(projectContext, config);

                    _logger.Info($"=== {result.ProjectType} Detection Results ===");
                    _logger.Info($"Project: {result.ProjectPath}");
                    _logger.Info($"Confidence: {result.Confidence:P2}");
                    _logger.Info($"Interpretation: {result.Interpretation}");

                    results.Add(result);
                }
            }

            return results;
        }
    }
}
