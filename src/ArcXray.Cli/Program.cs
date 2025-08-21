using ArcXray.Core;
using Microsoft.Extensions.DependencyInjection;

namespace ArcXray.Cli
{
    static class Program
    {
        static async Task Main(string[] args)
        {
            var rootCommand = CommandBuilder.Build(ExecuteAsync);

            var parseResult = rootCommand.Parse(args);

            await parseResult.InvokeAsync();
        }

        static async Task ExecuteAsync(string repoPath, string[] excludeKeywords, string detectionConfigPath)
        {
            var serviceCollection = DIConfiguration.Configure();

            var serviceProvider = serviceCollection.BuildServiceProvider();

            await serviceProvider
                .GetRequiredService<Pipeline>()
                .ExecuteAsync(repoPath, excludeKeywords, detectionConfigPath);
        }
    }
}
