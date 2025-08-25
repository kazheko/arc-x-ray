using ArcXray.Contracts.RepositoryStructure;

namespace ArcXray.Contracts.Reporting
{
    public interface IGenerateDiagram
    {
        Task<string> GenerateProjectDependencyGraphAsync(SolutionInfo solutionInfo);
    }
}
