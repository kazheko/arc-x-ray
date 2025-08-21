using ArcXray.Contracts.RepositoryStructure;

namespace ArcXray.Contracts.Application
{
    public interface ICheckExecutor
    {
        string CheckType { get; }
        Task<bool> ExecuteAsync(Check check, ProjectContext projectContext);
    }
}
