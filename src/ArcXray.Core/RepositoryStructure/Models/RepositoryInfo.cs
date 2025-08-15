namespace ArcXray.Core.RepositoryStructure.Models
{
    /// <summary>
    /// Represents a repository with .NET solution.
    /// </summary>
    public record RepositoryInfo(List<SolutionInfo> Solutions);
}
