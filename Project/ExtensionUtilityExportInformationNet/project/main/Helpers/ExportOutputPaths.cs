namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Root output folder of the export, shared by json, screenshots and toolpaths:
    /// the current process folder if it is writable (dev run),
    /// otherwise AppData\Local\ExportProjectInformation.
    /// </summary>
    public static class ExportOutputPaths
    {
        private const string OutputFolderName = "ExportProjectInformation";

        private static string? _root;

        /// <summary>
        /// Output root (resolved once per run).
        /// </summary>
        public static string Root => _root ??= ResolveRoot();

        private static string ResolveRoot()
        {
            var currentDir = Directory.GetCurrentDirectory();
            if (IsDirectoryWritable(currentDir))
                return currentDir;

            var appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                OutputFolderName);
            Directory.CreateDirectory(appDataDir);
            return appDataDir;
        }

        /// <summary>
        /// Checks write permission with a probe file (deleted immediately on close).
        /// </summary>
        private static bool IsDirectoryWritable(string directory)
        {
            try
            {
                var probePath = Path.Combine(directory, Path.GetRandomFileName());
                using (File.Create(probePath, 1, FileOptions.DeleteOnClose)) { }
                return true;
            }
            catch (Exception)
            {
                return false; // no write permission — fall back to AppData
            }
        }
    }
}
