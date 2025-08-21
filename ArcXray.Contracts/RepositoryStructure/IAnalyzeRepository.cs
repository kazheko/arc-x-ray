namespace ArcXray.Contracts.RepositoryStructure
{
    public interface IAnalyzeRepository
    {
        Task<RepositoryInfo> AnalyzeAsync(string repoPath, string[] excludeKeywords);
    }
}
