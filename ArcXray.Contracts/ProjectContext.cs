using System.IO;
using System.Xml.Linq;

namespace ArcXray.Contracts
{
    public class ProjectContext
    {
        public ProjectContext(string projectPath, string projectName)
        {
            ProjectPath = projectPath;
            ProjectName = projectName;
        }

        public string ProjectPath { get; private set; }
        public string ProjectName { get; private set; }
        public string Sdk { get; private set; } = string.Empty;
        public IEnumerable<string> TargetFrameworks { get; private set; } = Enumerable.Empty<string>();
        public IEnumerable<string> SourceFiles { get; private set; } = new List<string>();
        public XDocument ProjectFileContent { get; private set; } = new XDocument();
        public IEnumerable<string> ViewFiles { get; private set; } = new List<string>();
        public IEnumerable<string> AllFiles { get; private set; } = Enumerable.Empty<string>();
        public IEnumerable<PackageReference> PackageReferences { get; private set; } = new List<PackageReference>();
        public IEnumerable<ProjectReference> ProjectReferences { get; private set; } = new List<ProjectReference>();

        public void UpdateTargetFrameworks(IEnumerable<string> frameworks)
        {
            TargetFrameworks = frameworks;
        }

        public void UpdateProjectFileContent(XDocument document)
        {
            ProjectFileContent = document;
        }

        public void UpdateFiles(IEnumerable<string> files)
        {
            AllFiles = files;
        }

        public void UpdateSourceFiles(IEnumerable<string> files)
        {
            SourceFiles = files.Select(NormalizePath);
        }

        public void UpdateViewFiles(IEnumerable<string> files)
        {
            ViewFiles = files;
        }

        public void UpdateSdk(string? sdk)
        {
            Sdk = sdk ?? string.Empty;
        }

        public void UpdatePackageReference(IEnumerable<PackageReference> packageReferences)
        {
            PackageReferences = packageReferences;
        }

        public void UpdateProjectReferences(IEnumerable<ProjectReference> projectReferences)
        {
            ProjectReferences = projectReferences;
        }

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
