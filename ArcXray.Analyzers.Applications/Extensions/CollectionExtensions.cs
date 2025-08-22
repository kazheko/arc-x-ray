using System.Text.RegularExpressions;

namespace ArcXray.Analyzers.Applications.Extensions
{
    internal static class CollectionExtensions
    {
        /// <summary>
        /// Filters list of full file paths by wildcard pattern
        /// </summary>
        /// <param name="filePaths">List of full file paths</param>
        /// <param name="pattern">Wildcard pattern (e.g.: "Views/**/*.cshtml", "**/*.cs")</param>
        /// <returns>Filtered list of paths matching the pattern</returns>
        public static List<string> FilterByPattern(this IEnumerable<string> filePaths, string pattern)
        {
            if (filePaths == null)
                throw new ArgumentNullException(nameof(filePaths));

            if (string.IsNullOrEmpty(pattern))
                throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

            var regex = ConvertWildcardToRegex(pattern);

            return filePaths
                .Where(path => !string.IsNullOrEmpty(path) && IsMatch(path, regex))
                .ToList();
        }

        /// <summary>
        /// Converts wildcard pattern to regular expression
        /// </summary>
        /// <param name="pattern">Wildcard pattern</param>
        /// <returns>Regex object</returns>
        private static Regex ConvertWildcardToRegex(string pattern)
        {
            // Normalize pattern - replace \ with /
            var normalizedPattern = pattern.Replace('\\', '/');

            // Escape special regex characters except our wildcards
            var escaped = Regex.Escape(normalizedPattern);

            // Replace escaped wildcards back to regex patterns
            escaped = escaped
                .Replace("\\*\\*", "DOUBLE_ASTERISK_PLACEHOLDER")  // Temporary placeholder for **
                .Replace("\\*", "[^/]*")                           // * = any chars except /
                .Replace("DOUBLE_ASTERISK_PLACEHOLDER", ".*")      // ** = any chars including /
                .Replace("\\?", "[^/]");                           // ? = single char except /

            // Add anchors for exact matching
            var regexPattern = $"(?:^|/){escaped}$";

            return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
        }

        /// <summary>
        /// Checks if path matches the regex pattern
        /// </summary>
        /// <param name="filePath">Full file path</param>
        /// <param name="regex">Regular expression</param>
        /// <returns>True if path matches</returns>
        private static bool IsMatch(string filePath, Regex regex)
        {
            // Normalize path - replace \ with /
            var normalizedPath = filePath.NormalizePath();

            return regex.IsMatch(normalizedPath);
        }
    }
}
