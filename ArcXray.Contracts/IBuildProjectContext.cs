namespace ArcXray.Contracts
{
    public interface IBuildProjectContext
    {
        ProjectContext CreateFromCsproj(string csprojPath);
    }
}
