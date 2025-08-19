namespace ArcXray.Core.RepositoryStructure
{
    public interface IScanProjectFiles
    {
        IEnumerable<string> FindProjectFiles(string projectPath);
    }
}
