using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ArcXray.Analyzers.Applications.Checks
{
    /// <summary>
    /// Analyzes .csproj file content using XML parsing.
    /// Checks for specific XML elements, attributes, and their values.
    /// </summary>
    public class ProjectFileExecutor : ICheckExecutor
    {
        private readonly ILogger _logger;

        public ProjectFileExecutor(ILogger logger)
        {
            _logger = logger;
        }

        public string CheckType => "ProjectFile";

        /// <summary>
        /// Executes project file checks by parsing the .csproj XML structure.
        /// Supports checking attributes, elements, and their values with pattern matching.
        /// </summary>
        /// <param name="check">Check configuration with target path and expected values</param>
        /// <param name="projectContext">Project context containing project information</param>
        /// <returns>True if the check passes, false otherwise</returns>
        public Task<bool> ExecuteAsync(Check check, ProjectContext projectContext)
        {
            try
            {
                // Step 1: Determine what type of check we're performing based on the target format
                // Target can be:
                // - "Project/@Sdk" - checking an attribute (@ indicates attribute)
                // - "Project/PropertyGroup/TargetFramework" - checking an element value (path notation)

                bool checkResult;
                if (IsSdkCheck(check.Target))
                {
                    checkResult = check.ExpectedValues.Any(value => value.Equals(projectContext.Sdk, StringComparison.OrdinalIgnoreCase));
                }
                else if (IsFrameworkCheck(check.Target))
                {
                    if(string.IsNullOrEmpty(check.Pattern))
                    {
                        checkResult = projectContext.TargetFrameworks
                            .Intersect(check.ExpectedValues, StringComparer.OrdinalIgnoreCase)
                            .Any();
                    }
                    else
                    {
                        var regex = new Regex(check.Pattern, RegexOptions.Compiled);
                        checkResult = projectContext.TargetFrameworks.Any(regex.IsMatch);
                    }
                }
                else
                {
                    // Step 2: Parse the .csproj content.
                    // Determine what type of check we're performing.
                    throw new NotImplementedException();
                }

                return Task.FromResult(checkResult);
            }
            catch (Exception ex)
            {
                _logger.Error($"Class: {nameof(ProjectFileExecutor)}; Check: {check.Id}; Project: {projectContext.ProjectName}; Message: {ex.Message}.");

                return Task.FromResult(false);
            }
        }

        private bool CheckProjectFile(Check check, XDocument document)
        {
            if (document.Root == null)
            {
                // Invalid XML structure
                return false;
            }

            if (IsAttributeCheck(check.Target))
            {
                // This is an attribute check (contains @)
                return CheckAttribute(document, check);
            }

            // This is an element check (path to element)
            return CheckElement(document, check);
        }

        /// <summary>
        /// Determines if the target is checking for an attribute (contains @ symbol).
        /// </summary>
        /// <param name="target">Target path from check configuration</param>
        /// <returns>True if this is an attribute check</returns>
        private bool IsAttributeCheck(string target)
        {
            // Attribute checks use @ symbol
            // Example: "Project/@Sdk" means check the Sdk attribute of Project element
            return target.Contains("@");
        }

        /// <summary>
        /// Determines if the target is checking for an SDK
        /// </summary>
        /// <param name="target">Target path from check configuration</param>
        /// <returns>True if this is an SDK check</returns>
        private bool IsSdkCheck(string target)
        {
            return target.Equals("Project/@Sdk", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines if the target is checking for an Framework
        /// </summary>
        /// <param name="target">Target path from check configuration</param>
        /// <returns>True if this is an Framework check</returns>
        private bool IsFrameworkCheck(string target)
        {
            return target.Equals("Project/PropertyGroup/TargetFramework", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Checks for an attribute value in the XML document.
        /// Handles the format: "ElementName/@AttributeName"
        /// </summary>
        /// <param name="document">Parsed XML document</param>
        /// <param name="check">Check configuration</param>
        /// <returns>True if attribute matches expected value</returns>
        private bool CheckAttribute(XDocument document, Check check)
        {
            // Parse the target to extract element and attribute names
            // Format: "ElementName/@AttributeName"
            // Example: "Project/@Sdk" -> element: "Project", attribute: "Sdk"

            var parts = check.Target.Split('/');
            if (parts.Length < 2)
            {
                // Invalid format
                return false;
            }

            // First part is the element name
            var elementName = parts[0];

            // Second part is @AttributeName, remove the @ symbol
            var attributeName = parts[1].TrimStart('@');

            // Special case: if element is "Project", we check the root element
            XElement targetElement;
            if (elementName.Equals("Project", StringComparison.OrdinalIgnoreCase))
            {
                targetElement = document.Root;
            }
            else
            {
                // For other elements, search in the document
                // Note: This is a simple implementation, could be extended for nested paths
                targetElement = document.Root.Elements()
                    .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));
            }

            if (targetElement == null)
            {
                // Element not found
                return false;
            }

            // Get the attribute value
            var attributeValue = targetElement.Attribute(attributeName)?.Value;

            if (string.IsNullOrEmpty(attributeValue))
            {
                // Attribute doesn't exist or has no value
                return false;
            }

            // Check if we have an expected value to compare against
            if (check.ExpectedValues.Any())
            {
                // Exact match comparison (case-insensitive)
                // Example: Sdk="Microsoft.NET.Sdk.Web" should match "Microsoft.NET.Sdk.Web"
                return check.ExpectedValues.Any(v => v.Equals(attributeValue, StringComparison.OrdinalIgnoreCase));
            }

            // If no expected value specified, just check that attribute exists
            return true;
        }

        /// <summary>
        /// Checks for an element value in the XML document.
        /// Handles path notation like "Project/PropertyGroup/TargetFramework"
        /// </summary>
        /// <param name="document">Parsed XML document</param>
        /// <param name="check">Check configuration</param>
        /// <returns>True if element value matches expected value or pattern</returns>
        private bool CheckElement(XDocument document, Check check)
        {
            // Parse the path to navigate through XML structure
            // Format: "Parent/Child/GrandChild"
            // Example: "Project/PropertyGroup/TargetFramework"

            var pathParts = check.Target.Split('/');

            // Start from the root element
            XElement currentElement = document.Root;

            // Navigate through the path
            // Skip the first part if it's "Project" (the root)
            var startIndex = 0;
            if (pathParts[0].Equals("Project", StringComparison.OrdinalIgnoreCase))
            {
                startIndex = 1;
            }

            // Traverse the XML tree following the path
            for (int i = startIndex; i < pathParts.Length; i++)
            {
                var elementName = pathParts[i];

                // Find the child element with this name
                // Note: In .csproj files, namespace is usually not used, so we use LocalName
                currentElement = currentElement?.Elements()
                    .FirstOrDefault(e => e.Name.LocalName.Equals(elementName, StringComparison.OrdinalIgnoreCase));

                if (currentElement == null)
                {
                    // Path doesn't exist in the document
                    return false;
                }
            }

            // At this point, currentElement is the target element
            // Get its value (text content)
            var elementValue = currentElement.Value?.Trim();

            if (string.IsNullOrEmpty(elementValue))
            {
                // Element exists but has no value
                return false;
            }

            // Now check the value against expectations
            return CheckValue(elementValue, check);
        }

        /// <summary>
        /// Checks if an element value matches the expected value or pattern.
        /// Supports exact match, regex patterns, and simple existence checks.
        /// </summary>
        /// <param name="actualValue">The actual value from the XML element</param>
        /// <param name="check">Check configuration with expected value or pattern</param>
        /// <returns>True if the value matches expectations</returns>
        private bool CheckValue(string actualValue, Check check)
        {
            // Priority 1: Check for exact expected value match
            if (check.ExpectedValues.Any())
            {
                // Exact match (case-insensitive)
                // Example: TargetFramework value "net8.0" should match "net8.0"

                return check.ExpectedValues.Any(v => v.Equals(actualValue, StringComparison.OrdinalIgnoreCase));
            }

            // Priority 2: Check against regex pattern
            if (!string.IsNullOrEmpty(check.Pattern))
            {
                // Pattern matching using regex
                // Example: Pattern "^net[6-9]\\.\\d$" matches "net6.0", "net7.0", "net8.0", etc.
                try
                {
                    var regex = new Regex(check.Pattern, RegexOptions.IgnoreCase);
                    return regex.IsMatch(actualValue);
                }
                catch (ArgumentException)
                {
                    // Invalid regex pattern
                    // Could log this as a configuration error
                    return false;
                }
            }

            // Priority 3: If no expected value or pattern, just check that element has a value
            // This is useful for checking if an element exists with any non-empty value
            return !string.IsNullOrWhiteSpace(actualValue);
        }
    }
}