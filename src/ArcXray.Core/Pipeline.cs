using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.Reporting;
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
                _logger.Debug($"Processing solution: {solution.Name}, projects count: {solution.Projects.Count()}");

                //foreach (var project in solution.Projects)
                //{
                //    _logger.Debug($"Analyzing project at path: {project}");

                //    //var projectInfo = _buildProjectContext.BuildProjectInfo(projectPath);
                //    var projectContext = _buildProjectContext.BuildProjectContext(project.Value);

                //    var framework = projectContext.ProjectInfo.TargetFrameworks.First(); // todo: rework
                //    _logger.Debug($"Detected target framework: {framework}, SDK: {projectContext.ProjectInfo.Sdk}");

                //    var configs = await _provideDetectionConfig.GetConfigAsync(framework, projectContext.ProjectInfo.Sdk, detectionConfigPath);
                //    _logger.Debug($"Loaded {configs.Count()} detection configuration(s) for the project.");

                //    foreach (var config in configs)
                //    {
                //        _logger.Debug($"Running analysis with configuration: {config.Metadata.ProjectType}");

                //        var result = await _analyzeApplication.AnalyzeProjectAsync(projectContext, config);

                //        _logger.Info($"=== {result.ProjectType} Detection Results ===");
                //        _logger.Info($"Project: {result.ProjectPath}");
                //        _logger.Info($"Confidence: {result.Confidence:P2}");
                //        _logger.Info($"Interpretation: {result.Interpretation}");
                //    }
                //}

                var path2 = await _reportGenerator.AppendSolutionInfoAsync(solution);
            }

            _logger.Debug($"Analysis completed");
        }
    }
}
