namespace ArcXray.Core.RepositoryStructure.Models
{
    /// <summary>
    /// Represents a .NET project with its references and NuGet packages.
    /// </summary>
    public record ProjectInfo(
        string Name,
        string Path,
        List<string> ProjectReferences,
        List<PackageInfo> NugetPackages
    );
}
