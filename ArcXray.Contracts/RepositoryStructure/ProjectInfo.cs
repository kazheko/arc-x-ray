using System.Xml.Linq;

namespace ArcXray.Contracts.RepositoryStructure
{
    public class ProjectInfo
    {
        public ProjectInfo(string projectPath, string projectName)
        {
            ProjectPath = projectPath;
            ProjectName = projectName;
        }

        public string ProjectPath { get; private set; }
        public string ProjectName { get; private set; }
        public string Sdk { get; private set; } = string.Empty;
        public string Type { get; private set; } = string.Empty;
        public IEnumerable<string> TargetFrameworks { get; private set; } = Enumerable.Empty<string>();
        public XDocument ProjectFileContent { get; private set; } = new XDocument();
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
    }
}
