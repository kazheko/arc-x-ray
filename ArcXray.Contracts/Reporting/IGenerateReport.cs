using ArcXray.Contracts.RepositoryStructure;

namespace ArcXray.Contracts.Reporting
{
    public interface IGenerateReport
    {
        Task<string> AppendRepositoryInfoAsync(RepositoryInfo repositoryInfo);
        Task<string> AppendSolutionInfoAsync(SolutionInfo solutionInfo);
    }
}
