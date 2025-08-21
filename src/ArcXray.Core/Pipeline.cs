using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;


namespace ArcXray.Core
{
    public class Pipeline
    {
        private readonly IAnalyzeRepository _structureAnalyzer;
        private readonly IAnalyzeApplication _analyzeApplication;
        private readonly IBuildProjectContext _buildProjectContext;
        private readonly IProvideDetectionConfig _provideDetectionConfig;

        public Pipeline
        (
            IAnalyzeRepository structureAnalyzer, 
            IAnalyzeApplication analyzeApplication, 
            IBuildProjectContext buildProjectContext, 
            IProvideDetectionConfig provideDetectionConfig
        )
        {
            _structureAnalyzer = structureAnalyzer;
            _analyzeApplication = analyzeApplication;
            _buildProjectContext = buildProjectContext;
            _provideDetectionConfig = provideDetectionConfig;
        }

        public async Task ExecuteAsync(string repoPath, string[] excludeKeywords, string detectionConfigPath)
        {
            var structure = await _structureAnalyzer.AnalyzeAsync(repoPath, excludeKeywords);

            foreach (var solution in structure.Solutions)
            {
                foreach (var projectPath in solution.ProjectPaths)
                {
                    var context = _buildProjectContext.CreateFromCsproj(projectPath);

                    var configs = await _provideDetectionConfig.GetConfigAsync(context.Sdk, detectionConfigPath);

                    foreach (var config in configs)
                    {
                        var result = await _analyzeApplication.AnalyzeProjectAsync(context, config);
                        Console.WriteLine(result.Confidence);
                    }
                }
            }

        }
    }
}
