namespace ArcXray.Contracts
{
    public interface IFileRepository
    {
        IEnumerable<string> FindFiles(string rootPath, string searchPattern);
        bool IsDirectoryExist(string? path);
        string GetRelativePath(string relativeTo, string path);
        Task<string> ReadFileAsync(string filePath);
    }
}
