using System.Text.RegularExpressions;

namespace ArcXray.Analyzers.Applications.Extensions
{
    internal static class CollectionExtensions
    {
        /// <summary>
        /// Filters list of full file paths by wildcard pattern relative to root directory
        /// </summary>
        /// <param name="filePaths">List of full file paths</param>
        /// <param name="rootDir">Root directory to make paths relative to</param>
        /// <param name="pattern">Wildcard pattern (e.g.: "Views/**/*.cshtml", "**/*.cs")</param>
        /// <returns>Filtered list of paths matching the pattern</returns>
        public static List<string> FilterByPattern(this IEnumerable<string> filePaths, string rootDir, string pattern)
        {
            if (filePaths == null)
                throw new ArgumentNullException(nameof(filePaths));

            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

            if (string.IsNullOrEmpty(rootDir))
                throw new ArgumentException("Root directory cannot be null or empty", nameof(rootDir));

            var regex = ConvertWildcardToRegex(pattern);

            return filePaths
                .Where(path => !string.IsNullOrEmpty(path) && IsMatchRelativeToRoot(path, regex, rootDir))
                .ToList();
        }

        /// <summary>
        /// Converts wildcard pattern to regular expression for relative path matching
        /// </summary>
        /// <param name="pattern">Wildcard pattern</param>
        /// <returns>Regex object</returns>
        private static Regex ConvertWildcardToRegex(string pattern)
        {
            // Normalize pattern - replace \ with /
            pattern = pattern.Replace('\\', '/');

            // Replace escaped wildcards back to regex patterns
            pattern = Regex.Escape(pattern)
                .Replace(@"\*\*", "DOUBLE_STAR_PLACEHOLDER")
                .Replace(@"\*", "SINGLE_STAR_PLACEHOLDER")
                .Replace(@"\?", "QUESTION_PLACEHOLDER")
                .Replace(@"\(", "(")
                .Replace(@"\)", ")")
                .Replace(@"\|", "|");

            pattern = pattern
                .Replace("DOUBLE_STAR_PLACEHOLDER", ".*")  // ** - any cgars including /
                .Replace("SINGLE_STAR_PLACEHOLDER", "[^/]*")  // * - any chars except /
                .Replace("QUESTION_PLACEHOLDER", "[^/]");     // ? - single char except /

            // For relative path matching, pattern should match from start
            pattern = "^" + pattern + "$";

            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// Checks if path matches regex relative to root directory
        /// </summary>
        /// <param name="filePath">Full file path</param>
        /// <param name="regex">Compiled regex pattern</param>
        /// <param name="rootDir">Root directory</param>
        /// <returns>True if path matches</returns>
        private static bool IsMatchRelativeToRoot(string filePath, Regex regex, string rootDir)
        {
            var normalizedPath = filePath.Replace('\\', '/');
            var normalizedRootDir = rootDir.Replace('\\', '/');

            // Check if file is within root directory
            if (!normalizedPath.StartsWith(normalizedRootDir, StringComparison.OrdinalIgnoreCase))
                return false;

            // Get relative path by removing root directory
            var relativePath = normalizedPath.Substring(normalizedRootDir.Length);

            // Remove leading slash if present
            if (relativePath.StartsWith("/"))
                relativePath = relativePath.Substring(1);

            return regex.IsMatch(relativePath);
        }
    }
}
