using System.Xml.Linq;

namespace ArcXray.Contracts.RepositoryStructure
{
    public class ProjectContext
    {
        private readonly ProjectInfo _projectInfo;
        public ProjectContext(ProjectInfo projectInfo, IEnumerable<string> allFiles)
        {
            _projectInfo = projectInfo;
            AllFiles = allFiles.Select(NormalizePath);
        }

        public string ProjectPath => _projectInfo.ProjectPath;
        public string ProjectName => _projectInfo.ProjectName;
        public string Sdk => _projectInfo.Sdk;
        public string Type => _projectInfo.Type;
        public IEnumerable<string> TargetFrameworks => _projectInfo.TargetFrameworks;
        public IEnumerable<PackageReference> PackageReferences => _projectInfo.PackageReferences;
        public IEnumerable<ProjectReference> ProjectReferences => _projectInfo.ProjectReferences;
        public IEnumerable<string> AllFiles { get; private set; }

        /// <summary>
        /// Normalizes file paths for comparison.
        /// </summary>
        private string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            path = path.Replace('/', Path.DirectorySeparatorChar);
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            path = path.TrimEnd(Path.DirectorySeparatorChar);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                path = path.ToLowerInvariant();
            }

            return path;
        }
    }
}
