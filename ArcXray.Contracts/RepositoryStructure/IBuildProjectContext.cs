namespace ArcXray.Contracts.RepositoryStructure
{
    public interface IBuildProjectContext
    {
        ProjectInfo BuildProjectInfo(string csprojPath);
        ProjectContext BuildProjectContext(ProjectInfo projectInfo);
    }
}
