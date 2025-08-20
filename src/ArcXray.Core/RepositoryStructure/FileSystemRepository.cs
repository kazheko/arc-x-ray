using ArcXray.Contracts;

namespace ArcXray.Core.RepositoryStructure
{
    public class FileSystemRepository : IFileRepository
    {
        private const int MAX_FILE_SIZE_KB = 500;

        public bool IsDirectoryExist(string? path)
        {
            return Directory.Exists(path);
        }

        public IEnumerable<string> FindFiles(string rootPath, string searchPattern)
        {
            return Directory.EnumerateFiles(
                    rootPath,
                    searchPattern,
                    SearchOption.AllDirectories
                );
        }

        public string GetRelativePath(string relativeTo, string path)
        {
            return Path.GetRelativePath(relativeTo, path);
        }

        public async Task<string> ReadFileAsync(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            if (fileInfo.Length > MAX_FILE_SIZE_KB * 1024)
            {
                return string.Empty;
            }

            return await File.ReadAllTextAsync(filePath);
        }
    }
}
