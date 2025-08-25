using ArcXray.Contracts.Reporting;
using ArcXray.Contracts.RepositoryStructure;
using Scriban;

namespace ArcXray.Reporting.Markdown
{
    public class ReportGenerator : IGenerateReport
    {
        private readonly IGenerateDiagram _diagramGenerator;

        public ReportGenerator(IGenerateDiagram diagramGenerator)
        {
            _diagramGenerator = diagramGenerator;
        }

        public async Task<string> AppendRepositoryInfoAsync(RepositoryInfo repositoryInfo)
        {
            var data = await File.ReadAllTextAsync($"Templates/GeneralRepositoryInfo.sbn");

            var template = Template.Parse(data);

            var result = await template.RenderAsync(repositoryInfo);

            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            var path = $"output/discovery-report.md";

            await File.AppendAllTextAsync(path, result);

            return path;
        }

        public async Task<string> AppendSolutionInfoAsync(SolutionInfo solutionInfo)
        {
            var graphPath = await _diagramGenerator.GenerateProjectDependencyGraphAsync(solutionInfo);

            var data = await File.ReadAllTextAsync($"Templates/GeneralSolutionInfo.sbn");

            var template = Template.Parse(data);
            var result = template.Render(new
            {
                Name = solutionInfo.Name,
                Projects = solutionInfo.Projects.Values.Select(x => new
                {
                    Name = x.ProjectName,
                    IsEntryPoint = solutionInfo.RootProjectPaths.Contains(x.ProjectPath),
                    Type = x.Type,
                    Path = x.ProjectPath
                }),
                ProjDepsGraphPath = graphPath
            });

            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output");
            }

            var path = $"output/discovery-report.md";

            await File.AppendAllTextAsync(path, result);

            return path;
        }
    }
}
