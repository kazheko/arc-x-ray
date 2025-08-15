using ArcXray.Core.RepositoryStructure.Models;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ArcXray.Core.RepositoryStructure
{
    public static class RepositoryAnalyzer
    {
        public static RepositoryInfo Analyze(string repoPath, string[] excludeKeywords)
        {
            var slnFiles = Directory.GetFiles(repoPath, "*.sln", SearchOption.AllDirectories);
            var solutions = new List<SolutionInfo>();

            foreach (var sln in slnFiles)
            {
                var projectPaths = GetProjectPaths(sln)
                    .Where(p => !excludeKeywords.Any(kw => p.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                    .ToList();

                var projects = projectPaths
                    .Select(p => Parse(p))
                    .ToList();

                solutions.Add(new SolutionInfo(
                    Name: Path.GetFileNameWithoutExtension(sln),
                    Path: sln,
                    Projects: projects
                ));
            }

            return new RepositoryInfo(solutions);
        }

        /// <summary>
        /// Parses .sln files to find all .csproj file paths.
        /// Works for both SDK-style and older solutions.
        /// </summary>
        private static List<string> GetProjectPaths(string solutionPath)
        {
            var projectPaths = new List<string>();

            foreach (var line in File.ReadAllLines(solutionPath))
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

        /// <summary>
        /// Parses .csproj files and optionally packages.config to extract:
        /// - Project references
        /// - NuGet packages
        /// Supports both modern SDK-style projects and old .NET Framework projects.
        /// </summary>
        private static ProjectInfo Parse(string projectPath)
        {
            var doc = XDocument.Load(projectPath);

            // Extract project-to-project references
            var projectRefs = doc.Descendants("ProjectReference")
                .Select(x => x.Attribute("Include")?.Value ?? "")
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            // Extract NuGet packages from PackageReference
            var packages = doc.Descendants("PackageReference")
                .Select(x => new PackageInfo(
                    x.Attribute("Include")?.Value ?? "",
                    x.Attribute("Version")?.Value ?? ""
                ))
                .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                .ToList();

            // Support for old .NET Framework projects with packages.config
            var packagesConfigPath = Path.Combine(Path.GetDirectoryName(projectPath)!, "packages.config");
            if (File.Exists(packagesConfigPath))
            {
                var docPkg = XDocument.Load(packagesConfigPath);
                var oldPackages = docPkg.Descendants("package")
                    .Select(x => new PackageInfo(
                        x.Attribute("id")?.Value ?? "",
                        x.Attribute("version")?.Value ?? ""
                    ))
                    .Where(p => !string.IsNullOrWhiteSpace(p.Name))
                    .ToList();

                packages.AddRange(oldPackages);
            }

            return new ProjectInfo(
                Name: Path.GetFileNameWithoutExtension(projectPath),
                Path: projectPath,
                ProjectReferences: projectRefs,
                NugetPackages: packages
            );
        }
    }       
}
