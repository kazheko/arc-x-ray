using ArcXray.Contracts.RepositoryStructure;
using System.Xml.Linq;

namespace ArcXray.Analyzers.Projects.Structure
{
    internal static class CsprojParser
    {
        public static string? GetSdk(XDocument doc)
        {
            return doc.Root?.Attribute("Sdk")?.Value;
        }

        public static string[] GetTargetFrameworks(XDocument doc)
        {
            // Step 1: if project is multi-targets
            var tfms = doc.Descendants("TargetFrameworks")
                .FirstOrDefault()?.Value;

            if (!string.IsNullOrEmpty(tfms))
            {
                return tfms.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }

            // Step 2: check TargetFramework
            var tfm = doc.Descendants("TargetFramework")
                .FirstOrDefault()?.Value;

            if (!string.IsNullOrEmpty(tfm))
            {
                return new[] { tfm };
            }

            return Array.Empty<string>();
        }

        public static IEnumerable<PackageReference> GetPackageReferences(XDocument doc)
        {
            var root = doc.Root;

            if (root == null)
            {
                return Enumerable.Empty<PackageReference>();
            }

            // Find all PackageReference elements in any ItemGroup
            var packageRefs = root.Descendants("PackageReference");

            var refs = BuildPackageReferences(packageRefs);

            return refs;
        }

        public static IEnumerable<ProjectReference> GetProjectReferences(XDocument doc, string csprojPath)
        {
            var root = doc.Root;

            if (root == null)
            {
                return Enumerable.Empty<ProjectReference>();
            }

            var baseDir = Path.GetDirectoryName(csprojPath);

            // Find all ProjectReference elements
            var projectRefs = root.Descendants("ProjectReference");

            var refs = BuildProjectReferences(baseDir, projectRefs);

            return refs;
        }

        private static IEnumerable<ProjectReference> BuildProjectReferences(string? baseDir, IEnumerable<XElement> projectRefs)
        {
            foreach (var projectRef in projectRefs)
            {
                var relativePath = projectRef.Attribute("Include")?.Value;
                if (string.IsNullOrEmpty(relativePath))
                    continue;

                var path = Path.GetFullPath(Path.Combine(baseDir, relativePath));
                var name = Path.GetFileNameWithoutExtension(path);

                yield return new ProjectReference(path, name);
            }
        }

        private static IEnumerable<PackageReference> BuildPackageReferences(IEnumerable<XElement> packageRefs)
        {
            foreach (var packageRef in packageRefs)
            {
                // Get Include or Update attribute (Include for normal, Update for global packages)
                var name = packageRef.Attribute("Include")?.Value
                             ?? packageRef.Attribute("Update")?.Value;

                if (string.IsNullOrEmpty(name))
                    continue;

                // Version can be an attribute or a nested element
                var version = packageRef.Attribute("Version")?.Value
                                ?? packageRef.Element("Version")?.Value;

                yield return new PackageReference(name, version);
            }
        }
    }
}
