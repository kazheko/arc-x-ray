namespace ArcXray.Contracts
{
    public interface IFileRepository
    {
        IEnumerable<string> GetAllFiles(string rootPath, string searchPattern);
        bool IsDirectoryExist(string? path)
        string GetRelativePath(string relativeTo, string path);
    }
}
