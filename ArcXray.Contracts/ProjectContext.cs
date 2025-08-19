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
        public string? Sdk { get; private set; }
        public IEnumerable<string> SourceFiles { get; private set; } = new List<string>();
        public IEnumerable<string> ViewFiles { get; private set; } = new List<string>();
        public IEnumerable<string> AllFiles { get; private set; } = Enumerable.Empty<string>();
        public IEnumerable<PackageReference> PackageReferences { get; private set; } = new List<PackageReference>();
        public IEnumerable<ProjectReference> ProjectReferences { get; private set; } = new List<ProjectReference>();

        public void UpdateFiles(IEnumerable<string> files)
        {
            AllFiles = files;
        }

        public void UpdateSourceFiles(IEnumerable<string> files)
        {
            SourceFiles = files;
        }

        public void UpdateViewFiles(IEnumerable<string> files)
        {
            ViewFiles = files;
        }

        public void UpdateSdk(string? sdk)
        {
            Sdk = sdk;
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
