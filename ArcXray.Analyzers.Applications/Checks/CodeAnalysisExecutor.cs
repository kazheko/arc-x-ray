using ArcXray.Analyzers.Applications.Extensions;
using ArcXray.Contracts;
using ArcXray.Contracts.Application;
using ArcXray.Contracts.RepositoryStructure;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text.RegularExpressions;

namespace ArcXray.Analyzers.Applications.Checks
{
    /// <summary>
    /// Analyzes C# code structure using Roslyn syntax trees.
    /// Performs quick pre-filtering before building AST for performance.
    /// Supports various code analysis patterns: inheritance, attributes, return types, etc.
    /// </summary>
    public class CodeAnalysisExecutor : ICheckExecutor
    {
        private readonly ILogger _logger;
        private readonly IFileRepository _fileRepository;

        public CodeAnalysisExecutor(ILogger logger, IFileRepository fileRepository)
        {
            _logger = logger;
            _fileRepository = fileRepository;
        }

        public string CheckType => "CodeAnalysis";

        /// <summary>
        /// Executes code analysis checks on C# files.
        /// Uses two-phase approach: quick text search first, then detailed AST analysis if needed.
        /// </summary>
        /// <param name="check">Check configuration with analysis type and expected values</param>
        /// <param name="projectContext">Project context with file information</param>
        /// <returns>True if code pattern is found, false otherwise</returns>
        public async Task<bool> ExecuteAsync(Check check, ProjectContext projectContext)
        {
            try
            {
                // Step 1: Get target C# files to analyze
                var targetFiles = GetTargetFiles(check.Target, projectContext);

                if (!targetFiles.Any())
                {
                    // No files match the target pattern
                    return false;
                }

                // Step 2: Create a quick pre-filter based on the analysis type
                // This avoids expensive AST parsing for files that definitely don't contain what we're looking for
                var preFilter = CreatePreFilter(check);

                // Step 3: Analyze each file
                foreach (var filePath in targetFiles)
                {
                    var content = await _fileRepository.ReadFileAsync(filePath);

                    // Phase 1: Quick content check (if pre-filter exists)
                    if (preFilter != null && !preFilter(content))
                    {
                        // Quick check failed - this file definitely doesn't have what we're looking for
                        // Skip AST parsing for this file
                        continue;
                    }

                    // Phase 2: Detailed AST analysis
                    // Only reached if pre-filter passed or no pre-filter exists
                    var tree = CSharpSyntaxTree.ParseText(content);
                    var root = tree.GetRoot();

                    // Execute the appropriate analysis based on the type
                    bool found = ExecuteAnalysis(check, root);

                    if (found)
                    {
                        // Found what we're looking for - no need to check other files
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.Error($"Class: {nameof(CodeAnalysisExecutor)}; Check: {check.Id}; Project: {projectContext.ProjectName}; Message: {ex.Message}.");

                return false;
            }
        }

        /// <summary>
        /// Creates a pre-filter function for quick content checking.
        /// Returns null if no effective pre-filter can be created.
        /// </summary>
        /// <param name="check">Check configuration</param>
        /// <returns>Pre-filter function or null</returns>
        private Func<string, bool>? CreatePreFilter(Check check)
        {
            // todo: implement Aho–Corasick
            return content => check.ExpectedValues.Any(value => content.Contains(value, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Gets the list of C# files to analyze based on the target pattern.
        /// </summary>
        /// <param name="target">Target pattern (e.g., "Controllers/*.cs", "**/*.cs")</param>
        /// <param name="context">Project context with file lists</param>
        /// <returns>Collection of C# file paths to analyze</returns>
        private IEnumerable<string> GetTargetFiles(string target, ProjectContext context)
        {
            var files = new List<string>();

            // Use SourceFiles from context if available (already filtered to .cs files)
            var sourceFiles = context.AllFiles.Where(f => f.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));

            // Case 1: Wildcard patterns
            if (target.Contains("*"))
            {
                // Handle recursive search (**/*.cs)
                if (target.StartsWith("**"))
                {
                    // All C# files recursively
                    files.AddRange(sourceFiles);
                }
                else
                {
                    // Example of target with wildcard (Controllers/*.cs)
                    // Filter files in the specific directory
                    var filteredFiles = context.AllFiles.FilterByPattern(context.ProjectPath, target);
                    files.AddRange(filteredFiles);
                }
            }
            // Case 2: Directory path (all .cs files in directory)
            else if (IsDirectory(target, context))
            {
                var fullDirectory = Path.Combine(context.ProjectPath, target);
                var normalizedDir = fullDirectory.NormalizePath() + Path.DirectorySeparatorChar;

                // All C# files in the specified directory and subdirectories
                var filteredFiles = sourceFiles
                    .Where(file => file.EndsWith(".cs"))
                    .Select(file => Path.GetDirectoryName(file).NormalizePath())
                    .Where(dir => !string.IsNullOrEmpty(dir) && dir.StartsWith(normalizedDir, StringComparison.OrdinalIgnoreCase));

                files.AddRange(filteredFiles);
            }
            // Case 3: Specific file
            else
            {
                var fullPath = Path.IsPathRooted(target)
                    ? target
                    : Path.Combine(context.ProjectPath, target);

                var normalizedPath = fullPath.NormalizePath();

                var matchingFile = sourceFiles.FirstOrDefault(file =>
                    file.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase));

                if (!string.IsNullOrEmpty(matchingFile))
                {
                    files.Add(matchingFile);
                }
            }

            return files;
        }

        private static bool IsDirectory(string target, ProjectContext context)
        {
            if(target.EndsWith("/") || target.EndsWith("\\"))
            {
                return true;
            }

            var fullPath = Path.Combine(context.ProjectPath, target);
            var normalizedDir = fullPath.NormalizePath() + Path.DirectorySeparatorChar;

            return context.AllFiles.Any(file => file.StartsWith(normalizedDir));
        }

        /// <summary>
        /// Executes the appropriate analysis based on the analysis type.
        /// Delegates to specific analysis methods.
        /// </summary>
        private bool ExecuteAnalysis(Check check, SyntaxNode root)
        {
            return check.AnalysisType switch
            {
                "ClassInheritance" => CheckClassInheritance(root, check),
                "ClassAttribute" => CheckClassAttribute(root, check),
                "MethodAttribute" => CheckMethodAttribute(root, check),
                "MethodReturnType" => CheckMethodReturnType(root, check),
                "ParameterAttribute" => CheckParameterAttribute(root, check),
                "PropertyAttribute" => CheckPropertyAttribute(root, check),
                _ => false
            };
        }

        /// <summary>
        /// Checks if any class in the file inherits from the expected base class.
        /// Example: Checking if controllers inherit from ControllerBase.
        /// </summary>
        private bool CheckClassInheritance(SyntaxNode root, Check check)
        {
            // Find all class declarations in the syntax tree
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classes)
            {
                // Check if class has a base list (inheritance or interface implementation)
                if (classDecl.BaseList == null)
                    continue;

                // Check each base type
                foreach (var baseType in classDecl.BaseList.Types)
                {
                    var typeName = baseType.Type.ToString();

                    // Check for exact match or qualified name match
                    // Examples: "ControllerBase" or "Microsoft.AspNetCore.Mvc.ControllerBase"
                    if (check.ExpectedValues.Any(value => value.Equals(typeName, StringComparison.OrdinalIgnoreCase)
                        || check.ExpectedValues.Any(value => typeName.EndsWith($".{value}", StringComparison.OrdinalIgnoreCase))))

                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if any class has the expected attribute.
        /// Example: Checking for [ApiController] attribute on controller classes.
        /// </summary>
        private bool CheckClassAttribute(SyntaxNode root, Check check)
        {
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classDecl in classes)
            {
                // Check all attribute lists on the class
                foreach (var attrList in classDecl.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = attr.Name.ToString();

                        var detected = check.ExpectedValues.Any(atr => MatchesAttribute(attrName, atr));

                        if (detected)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if any method has one of the expected attributes.
        /// Example: Checking for [HttpGet], [HttpPost] on action methods.
        /// </summary>
        private bool CheckMethodAttribute(SyntaxNode root, Check check)
        {
            // Get all method declarations
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            // Build list of expected attributes

            if (!check.ExpectedValues.Any())
                return false;

            foreach (var method in methods)
            {
                // Check all attribute lists on the method
                foreach (var attrList in method.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = attr.Name.ToString();

                        // Check if this attribute matches any of the expected ones
                        if (check.ExpectedValues.Any(expected => MatchesAttribute(attrName, expected)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if any method returns one of the expected types.
        /// Example: Checking for methods returning IActionResult or Task<ActionResult>.
        /// </summary>
        private bool CheckMethodReturnType(SyntaxNode root, Check check)
        {
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            if (!check.ExpectedValues.Any())
                return false;

            foreach (var method in methods)
            {
                var returnType = method.ReturnType.ToString();

                // Check if return type contains any of the expected types
                // This handles generic types like Task<IActionResult>
                var matched = check.ExpectedValues.Any(value => returnType.Contains(value, StringComparison.OrdinalIgnoreCase));
                if (matched)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if any method parameter has one of the expected attributes.
        /// Example: Checking for [FromBody], [FromQuery] on action parameters.
        /// </summary>
        private bool CheckParameterAttribute(SyntaxNode root, Check check)
        {
            // Get all parameters from all methods
            var parameters = root.DescendantNodes().OfType<ParameterSyntax>();

            if (!check.ExpectedValues.Any())
                return false;

            foreach (var param in parameters)
            {
                foreach (var attrList in param.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = attr.Name.ToString();

                        if (check.ExpectedValues.Any(expected => MatchesAttribute(attrName, expected)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if any property has one of the expected attributes.
        /// Example: Checking for [Required], [Range] on model properties.
        /// </summary>
        private bool CheckPropertyAttribute(SyntaxNode root, Check check)
        {
            // Get all properties
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();

            if (!check.ExpectedValues.Any())
                return false;

            foreach (var prop in properties)
            {
                foreach (var attrList in prop.AttributeLists)
                {
                    foreach (var attr in attrList.Attributes)
                    {
                        var attrName = attr.Name.ToString();

                        if (check.ExpectedValues.Any(expected => MatchesAttribute(attrName, expected)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Matches attribute names with various formats.
        /// Handles: "HttpGet" vs "HttpGetAttribute" vs "Microsoft.AspNetCore.Mvc.HttpGet"
        /// </summary>
        private bool MatchesAttribute(string actualName, string expectedName)
        {
            if (string.IsNullOrEmpty(expectedName))
                return false;

            // Case 1: Exact match
            if (actualName.Equals(expectedName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Case 2: Expected name + "Attribute" suffix
            if (actualName.Equals(expectedName + "Attribute", StringComparison.OrdinalIgnoreCase))
                return true;

            // Case 3: Qualified name ending with expected name
            if (actualName.EndsWith("." + expectedName, StringComparison.OrdinalIgnoreCase))
                return true;

            // Case 4: Qualified name ending with expected name + "Attribute"
            if (actualName.EndsWith("." + expectedName + "Attribute", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}