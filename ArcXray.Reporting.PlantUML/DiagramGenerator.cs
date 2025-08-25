using ArcXray.Contracts.Reporting;
using ArcXray.Contracts.RepositoryStructure;
using Scriban;

namespace ArcXray.Reporting.PlantUML
{
    public class DiagramGenerator : IGenerateDiagram
    {
        public async Task<string> GenerateProjectDependencyGraph(SolutionInfo solutionInfo)
        {
            var data = await File.ReadAllTextAsync($"Templates/ProjectDependencyGraph.sbn");

            var template = Template.Parse(data);
            var result = template.Render(new
            {
                Solution = solutionInfo.Name,
                Projects = solutionInfo.Projects.Values.Select(x=>new {
                    Name = x.ProjectName, 
                    Sdk = x.Sdk, 
                    Refs = x.ProjectReferences.Select(r=>r.ProjectName)
                })
            });

            if (!Directory.Exists("output"))
            {
                Directory.CreateDirectory("output/solutions");
            }

            var path = $"output/{solutionInfo.Name.Replace('.', '-').ToLower()}-proj-deps-graph.pu";

            await File.WriteAllTextAsync(path, result);

            return path;
        }
    }
}