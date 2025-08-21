using ArcXray.Contracts;
using ArcXray.Contracts.RepositoryStructure;
using System.Text.RegularExpressions;

namespace ArcXray.Core.RepositoryStructure
{
    public class RepositoryAnalyzer : IAnalyzeRepository
    {
        private readonly IFileRepository _fileRepository;

        public RepositoryAnalyzer(IFileRepository fileRepository)
        {
            _fileRepository = fileRepository;
        }

        public async Task<RepositoryInfo> AnalyzeAsync(string repoPath, string[] excludeKeywords)
        {
            var slnFiles = _fileRepository.FindFiles(repoPath, "*.sln");
            var solutions = new List<SolutionInfo>();

            foreach (var sln in slnFiles)
            {
                var projectPaths = await GetProjectPathsAsync(sln);

                projectPaths = projectPaths
                    .Where(p => !excludeKeywords.Any(kw => p.Contains(kw, StringComparison.OrdinalIgnoreCase)))
  ;

                solutions.Add(new SolutionInfo(
                    Name: Path.GetFileNameWithoutExtension(sln),
                    Path: sln,
                    ProjectPaths: projectPaths
                ));
            }

            return new RepositoryInfo(repoPath, solutions);
        }

        /// <summary>
        /// Parses .sln files to find all .csproj file paths.
        /// Works for both SDK-style and older solutions.
        /// </summary>
        private async Task<IEnumerable<string>> GetProjectPathsAsync(string solutionPath)
        {
            var projectPaths = new List<string>();

            var content = await _fileRepository.ReadFileAsync(solutionPath);
            var lines = content.Split(['\n'], StringSplitOptions.None);

            foreach (var line in lines)
            {
                // Example line in .sln:
                // Project("{GUID}") = "MyProject", "MyProject\MyProject.csproj", "{GUID}"
                if (line.Contains(".csproj", StringComparison.OrdinalIgnoreCase))
                {
                    var match = Regex.Match(line, @"""([^""]+\.csproj)""");
                    if (match.Success)
                    {
                        var relativePath = match.Groups[1].Value;
                        var fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(solutionPath)!, relativePath));
                        projectPaths.Add(fullPath);
                    }
                }
            }

            return projectPaths;
        }
    }       
}
