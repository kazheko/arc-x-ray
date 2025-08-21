using ArcXray.Analyzers.Applications;
using ArcXray.Analyzers.Projects.Structure;
using ArcXray.Cli.Loggers;
using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;
using ArcXray.Core;
using ArcXray.Core.RepositoryStructure;
using Microsoft.Extensions.DependencyInjection;

namespace ArcXray.Cli
{
    internal static class DIConfiguration
    {
        public static IServiceCollection Configure()
        {
            // 1. Create a ServiceCollection
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddTransient<ILogger, ConsoleLogger>();
            serviceCollection.AddTransient<IFileRepository, FileSystemRepository>();
            serviceCollection.AddTransient<IProvideDetectionConfig, DetectionConfigProvider>();
            serviceCollection.AddTransient<IProvideDetectionConfig, DetectionConfigProvider>();
            serviceCollection.AddTransient<IAnalyzeRepository, RepositoryAnalyzer>();

            serviceCollection.AddTransient<ApplicationAnalyzer>();
            serviceCollection.AddTransient<IAnalyzeApplication>(provider =>
            {
                var originalService = provider.GetService<ApplicationAnalyzer>();
                return new AppAnalysisLogger(originalService);
            });
            serviceCollection.AddTransient<IBuildProjectContext, ProjectContextBuilder>();
            serviceCollection.AddTransient<Pipeline>();

            return serviceCollection;
        }
    }
}
