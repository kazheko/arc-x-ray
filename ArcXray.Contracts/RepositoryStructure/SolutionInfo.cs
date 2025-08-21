namespace ArcXray.Contracts.RepositoryStructure
{
    /// <summary>
    /// Represents a .NET solution with a list of its projects.
    /// </summary>
    public record SolutionInfo(
        string Name,
        string Path,
        IEnumerable<string> ProjectPaths
    );
}
