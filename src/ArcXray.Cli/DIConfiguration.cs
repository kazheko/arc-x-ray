using ArcXray.Analyzers.Applications;
using ArcXray.Analyzers.Projects.Structure;
using ArcXray.Cli.Loggers;
using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.Reporting;
using ArcXray.Contracts.RepositoryStructure;
using ArcXray.Core;
using ArcXray.Core.RepositoryStructure;
using ArcXray.Reporting.Markdown;
using ArcXray.Reporting.PlantUML;
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
            serviceCollection.AddTransient<IProvideCheckList, CheckListProvider>();

            serviceCollection.AddTransient<IAnalyzeRepository, RepositoryAnalyzer>();

            serviceCollection.AddTransient<ApplicationAnalyzer>();
            serviceCollection.AddTransient<IAnalyzeApplication>(provider =>
            {
                var originalService = provider.GetService<ApplicationAnalyzer>();
                return new AppAnalysisLogger(originalService);
            });
            serviceCollection.AddTransient<IBuildProjectContext, ProjectContextBuilder>();

            serviceCollection.AddTransient<IGenerateDiagram, DiagramGenerator>();
            serviceCollection.AddTransient<IGenerateReport, ReportGenerator>();

            serviceCollection.AddTransient<Pipeline>();

            return serviceCollection;
        }
    }
}
