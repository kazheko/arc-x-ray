using ArcXray.Contracts;
using ArcXray.Contracts.RepositoryStructure;
using System.Xml.Linq;

namespace ArcXray.Analyzers.Projects.Structure
{
    public class ProjectContextBuilder : IBuildProjectContext
    {
        private static readonly HashSet<string> ExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
           "bin", "obj", ".vs", ".git", "node_modules", "packages", ".idea", "TestResults"
        };

        private readonly ILogger _logger;
        private readonly IFileRepository _fileRepository;

        public ProjectContextBuilder(ILogger logger, IFileRepository fileRepository)
        {
            _logger = logger;
            _fileRepository = fileRepository;
        }

        public ProjectInfo BuildProjectInfo(string csprojPath)
        {
            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"Project file not found: {csprojPath}");

            var projectInfo = new ProjectInfo
            (
                projectPath: Path.GetDirectoryName(csprojPath) ?? string.Empty,
                projectName: Path.GetFileNameWithoutExtension(csprojPath)
            );

            try
            {
                LoadCsprojDetails(csprojPath, projectInfo);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing project file: {ex.Message}");
            }

            return projectInfo;
        }

        public ProjectContext BuildProjectContext(ProjectInfo projectInfo)
        {
            var allFiles = FindProjectFiles(projectInfo.ProjectPath);
            return new ProjectContext(projectInfo, allFiles);
        }

        private static void LoadCsprojDetails(string csprojPath, ProjectInfo projectInfo)
        {
            // Load and parse XML
            var doc = XDocument.Load(csprojPath);
            projectInfo.UpdateProjectFileContent(doc);

            // Load project Target Frameworks
            var frameworks = CsprojParser.GetTargetFrameworks(doc);
            projectInfo.UpdateTargetFrameworks(frameworks);

            // Load project SDK
            var sdk = CsprojParser.GetSdk(doc);
            projectInfo.UpdateSdk(sdk);

            // Load package references
            var packRefs = CsprojParser.GetPackageReferences(doc);
            projectInfo.UpdatePackageReference(packRefs);

            // Load project references
            var projRefs = CsprojParser.GetProjectReferences(doc, csprojPath);
            projectInfo.UpdateProjectReferences(projRefs);
        }

        private IEnumerable<string> FindProjectFiles(string projectPath)
        {
            try
            {
                var allFiles = _fileRepository.FindFiles(projectPath, "*.*");

                return allFiles.Where(file => !IsInExcludedFolder(projectPath, file)).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error($"Error scanning project files: {ex.Message}");
            }

            return Enumerable.Empty<string>();
        }

        private bool IsInExcludedFolder(string projectPath, string filePath)
        {
            var relativePath = _fileRepository.GetRelativePath(projectPath, filePath);

            var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return parts.Any(ExcludedFolders.Contains);
        }
    }
}