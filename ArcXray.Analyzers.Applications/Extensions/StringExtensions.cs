namespace ArcXray.Analyzers.Applications.Extensions
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Normalizes file paths for comparison.
        /// </summary>
        public static string NormalizePath(this string path)
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
