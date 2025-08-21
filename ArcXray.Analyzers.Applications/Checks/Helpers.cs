namespace ArcXray.Analyzers.Applications.Checks
{
    internal static class Helpers
    {
        /// <summary>
        /// Converts a wildcard pattern to a regular expression.
        /// * becomes .* (any characters)
        /// ? becomes . (single character)
        /// </summary>
        public static string WildcardToRegex(string pattern)
        {
            // Escape special regex characters except * and ?
            string escaped = System.Text.RegularExpressions.Regex.Escape(pattern);

            // Replace escaped wildcards with regex equivalents
            escaped = escaped.Replace("\\*", ".*");
            escaped = escaped.Replace("\\?", ".");

            // Anchor the pattern to match the whole string
            return "^" + escaped + "$";
        }

        /// <summary>
        /// Normalizes file paths for comparison.
        /// </summary>
        public static string NormalizePath(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            path = path.Replace('/', Path.DirectorySeparatorChar);
            path = path.Replace('\\', Path.DirectorySeparatorChar);
            path = path.TrimEnd(Path.DirectorySeparatorChar);

            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                path = path.ToLowerInvariant();
            }

            return path;
        }

    }
}
