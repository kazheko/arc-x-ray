using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;
using System.Text.RegularExpressions;

namespace ArcXray.Analyzers.Applications.Checks
{
    /// <summary>
    /// Checks for NuGet package references in the project.
    /// Uses pre-loaded package information from ProjectContext to avoid parsing .csproj files.
    /// </summary>
    public class NuGetPackageExecutor : ICheckExecutor
    {
        private readonly ILogger _logger;

        public NuGetPackageExecutor(ILogger logger)
        {
            _logger = logger;
        }

        public string CheckType => "NuGetPackage";

        /// <summary>
        /// Executes the NuGet package check against the project context.
        /// Checks if specified package(s) are referenced in the project.
        /// </summary>
        /// <param name="check">Check configuration containing target package and alternatives</param>
        /// <param name="projectContext">Project context with pre-loaded package references</param>
        /// <returns>True if the package (or any alternative) is found, false otherwise</returns>
        public Task<bool> ExecuteAsync(Check check, ProjectContext projectContext)
        {
            try
            {
                // Step 1: Check if the main target package exists
                bool mainPackageFound = CheckPackageExists(check.Target, projectContext);

                if (mainPackageFound)
                {
                    // Found the main package, no need to check alternatives
                    return Task.FromResult(true);
                }

                // Step 2: If main package not found, check alternative packages
                // This is useful for packages that have been renamed or have multiple variants
                // Example: "Microsoft.AspNetCore.Mvc.Versioning" was renamed to "Asp.Versioning.Mvc"
                if (check.AlternativeTargets != null && check.AlternativeTargets.Any())
                {
                    foreach (var alternativePackage in check.AlternativeTargets)
                    {
                        bool altPackageFound = CheckPackageExists(alternativePackage, projectContext);
                        if (altPackageFound)
                        {
                            // Found an alternative package
                            return Task.FromResult(true);
                        }
                    }
                }

                // Step 3: Special case - check for implicit framework references
                // Some packages like "Microsoft.AspNetCore.App" might be included as FrameworkReference
                // rather than PackageReference in modern .NET projects
                bool isFrameworkReference = CheckIfFrameworkReference(check.Target, projectContext);
                if (isFrameworkReference)
                {
                    return Task.FromResult(true);
                }

                // No package found in any form
                return Task.FromResult(false);
            }
            catch (Exception ex)
            {
                _logger.Error($"Class: {nameof(NuGetPackageExecutor)}; Check: {check.Id}; Project: {projectContext.ProjectName}; Message: {ex.Message}.");

                // Return false on any error to avoid false positives
                return Task.FromResult(false);
            }
        }

        /// <summary>
        /// Checks if a specific package exists in the project's package references.
        /// Handles different matching strategies for flexibility.
        /// </summary>
        /// <param name="packageName">Name of the package to search for</param>
        /// <param name="context">Project context containing package references</param>
        /// <returns>True if package is found, false otherwise</returns>
        private bool CheckPackageExists(string packageName, ProjectContext context)
        {
            // Early return if no packages are loaded
            if (context.PackageReferences == null || !context.PackageReferences.Any())
            {
                return false;
            }

            // Strategy 1: Exact match (case-insensitive)
            // Most common case - package name matches exactly
            // Example: "Swashbuckle.AspNetCore" == "Swashbuckle.AspNetCore"
            bool exactMatch = context.PackageReferences.Any(pkg =>
                pkg.Name.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (exactMatch)
            {
                return true;
            }

            // Strategy 2: Check for package groups/meta-packages
            // Some packages are meta-packages that include multiple sub-packages
            // Example: Looking for "Swashbuckle.AspNetCore" might need to match:
            // - "Swashbuckle.AspNetCore.Swagger"
            // - "Swashbuckle.AspNetCore.SwaggerGen"
            // - "Swashbuckle.AspNetCore.SwaggerUI"
            bool isMetaPackage = IsMetaPackagePattern(packageName);
            if (isMetaPackage)
            {
                // Check if any referenced package starts with the meta-package name
                bool subPackageFound = context.PackageReferences.Any(pkg =>
                    pkg.Name.StartsWith(packageName + ".", StringComparison.OrdinalIgnoreCase));

                if (subPackageFound)
                {
                    return true;
                }
            }

            // Strategy 3: Handle wildcard patterns if specified
            // Example: "Microsoft.EntityFrameworkCore.*" to match any EF Core package
            if (packageName.Contains("*"))
            {
                var rexexPattern = Helpers.WildcardToRegex(packageName);
                var regex = new Regex(rexexPattern, RegexOptions.IgnoreCase);

                var matched = context.PackageReferences.Any(pkg => regex.IsMatch(pkg.Name));

                if (matched)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a package might be included as a framework reference.
        /// In modern .NET, some packages are included via FrameworkReference instead of PackageReference.
        /// </summary>
        /// <param name="packageName">Name of the package to check</param>
        /// <param name="context">Project context</param>
        /// <returns>True if it's a known framework reference, false otherwise</returns>
        private bool CheckIfFrameworkReference(string packageName, ProjectContext context)
        {
            // List of known packages that might be FrameworkReferences in modern .NET
            // These are typically included differently in .NET 5+ projects
            var knownFrameworkReferences = new[]
            {
                "Microsoft.AspNetCore.App",
                "Microsoft.WindowsDesktop.App",
                "Microsoft.NETCore.App"
            };

            // Check if the package is a known framework reference
            bool isKnownFramework = knownFrameworkReferences.Any(fr =>
                fr.Equals(packageName, StringComparison.OrdinalIgnoreCase));

            if (!isKnownFramework)
            {
                return false;
            }

            // For Web API detection, we're particularly interested in Microsoft.AspNetCore.App
            // This is implicitly included when SDK is Microsoft.NET.Sdk.Web
            if (packageName.Equals("Microsoft.AspNetCore.App", StringComparison.OrdinalIgnoreCase))
            {
                // Check if the project uses Web SDK
                return context.Sdk?.Equals("Microsoft.NET.Sdk.Web", StringComparison.OrdinalIgnoreCase) ?? false;
            }

            // For other framework references, we'd need additional context
            // For now, return false for unknown framework references
            return false;
        }

        /// <summary>
        /// Determines if a package name represents a meta-package pattern.
        /// Meta-packages are packages that are typically split into multiple sub-packages.
        /// </summary>
        /// <param name="packageName">Package name to check</param>
        /// <returns>True if it's likely a meta-package pattern</returns>
        private bool IsMetaPackagePattern(string packageName)
        {
            // Known meta-packages that are often split into sub-packages
            var knownMetaPackages = new[]
            {
                "Swashbuckle.AspNetCore",
                "Microsoft.EntityFrameworkCore",
                "Microsoft.Extensions.Logging",
                "Microsoft.Extensions.Configuration",
                "Serilog.AspNetCore",
                "AutoMapper.Extensions",
                "FluentValidation.AspNetCore"
            };

            return knownMetaPackages.Any(meta =>
                meta.Equals(packageName, StringComparison.OrdinalIgnoreCase));
        }
    }
}