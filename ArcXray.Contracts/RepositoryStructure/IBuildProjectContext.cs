namespace ArcXray.Contracts.RepositoryStructure
{
    public interface IBuildProjectContext
    {
        ProjectContext CreateFromCsproj(string csprojPath);
    }
}
