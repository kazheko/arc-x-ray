namespace ArcXray.Core.RepositoryStructure.Models
{
    /// <summary>
    /// Represents a .NET solution with a list of its projects.
    /// </summary>
    public record SolutionInfo(
        string Name,
        string Path,
        List<ProjectInfo> Projects
    );
}
