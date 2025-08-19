using ArcXray.Contracts;
using System.Xml.Linq;

namespace ArcXray.Analyzers.Projects.Structure
{
    public class ProjectContextBuilder : IBuildProjectContext
    {
        private static readonly HashSet<string> ExcludedFolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
           "bin", "obj", ".vs", ".git", "node_modules", "packages", ".idea", "TestResults"
        };

        private static readonly HashSet<string> SourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
           ".cs"
        };

        private static readonly HashSet<string> ViewExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
           ".cshtml", ".razor"
        };

        private readonly ILogger _logger;
        private readonly IFileRepository _fileRepository;

        public ProjectContextBuilder(ILogger logger, IFileRepository fileRepository)
        {
            _logger = logger;
            _fileRepository = fileRepository;
        }

        public ProjectContext CreateFromCsproj(string csprojPath)
        {
            if (!File.Exists(csprojPath))
                throw new FileNotFoundException($"Project file not found: {csprojPath}");

            var context = new ProjectContext
            (
                projectPath: Path.GetDirectoryName(csprojPath) ?? string.Empty,
                projectName: Path.GetFileNameWithoutExtension(csprojPath)
            );

            try
            {
                LoadCsprojDetails(csprojPath, context);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing project file: {ex.Message}");
            }

            // Load project files
            LoadProjectFiles(csprojPath, context);

            return context;
        }

        private static void LoadCsprojDetails(string csprojPath, ProjectContext context)
        {
            // Load and parse XML
            var doc = XDocument.Load(csprojPath);

            // Load project SDK
            var sdk = CsprojParser.GetSdk(doc);
            context.UpdateSdk(sdk);

            // Load package references
            var packRefs = CsprojParser.GetPackageReferences(doc);
            context.UpdatePackageReference(packRefs);

            // Load project references
            var projRefs = CsprojParser.GetProjectReferences(doc, csprojPath);
            context.UpdateProjectReferences(projRefs);
        }

        private void LoadProjectFiles(string csprojPath, ProjectContext context)
        {
            var allFiles = FindProjectFiles(csprojPath);
            context.UpdateFiles(allFiles);

            var sourceFiles = allFiles.Where(file => SourceExtensions.Contains(GetFileExtension(file)));
            context.UpdateSourceFiles(sourceFiles);

            var viewFiles = allFiles.Where(file => ViewExtensions.Contains(GetFileExtension(file)));
            context.UpdateViewFiles(viewFiles);
        }

        private static string GetFileExtension(string filePath)
        {
            return filePath.Split('.').Last();
        }

        private IEnumerable<string> FindProjectFiles(string projectPath)
        {
            try
            {
                var allFiles = _fileRepository.GetAllFiles(projectPath, "*.*");

                return allFiles.Where(file => IsInExcludedFolder(projectPath, file));
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