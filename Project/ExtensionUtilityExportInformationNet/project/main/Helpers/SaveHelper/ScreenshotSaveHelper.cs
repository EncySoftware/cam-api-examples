using CAMAPI.DotnetHelper;
using CAMAPI.Project;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Saves screenshots extracted from the .stcp project file into the json.
    /// </summary>
    public static class ScreenshotSaveHelper
    {
        private const string ScreenshotsFolderName = "screenshots";

        private static JsonBuilder? _jsonBuilder;

        /// <summary>
        /// Initializes the JSON builder for saving screenshots data.
        /// </summary>
        public static void Initialize(JsonBuilder builder)
        {
            _jsonBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Writes the Screenshots section (an empty array — the project has no screenshots)
        /// and saves the images into the screenshots subfolder inside
        /// <paramref name="outputDirectory"/> (the folder of the resulting json).
        /// An unsaved project gets no section; an extraction error does not fail
        /// the export — ScreenshotsError is written instead.
        /// </summary>
        public static void SaveScreenshots(ComWrapper<ICamApiProject> projectCom, string outputDirectory)
        {
            var projectFilePath = ProjectHelper.FilePath(projectCom);
            if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
                return;

            try
            {
                var screenshots = ProjectScreenshotExtractor.Extract(projectFilePath);
                _jsonBuilder?.BeginArray("Screenshots");
                if (screenshots.Count > 0)
                {
                    var screenshotsDir = PrepareScreenshotsFolder(outputDirectory);
                    foreach (var screenshot in screenshots)
                        SaveScreenshot(screenshot, screenshotsDir);
                }
                _jsonBuilder?.EndArray();
            }
            catch (Exception e)
            {
                _jsonBuilder?.AddStrPair("ScreenshotsError", e.GetType().Name + ": " + e.Message);
            }
        }

        /// <summary>
        /// Creates the screenshots folder next to the json (or cleans it from the previous export).
        /// </summary>
        private static string PrepareScreenshotsFolder(string outputDirectory)
        {
            var screenshotsDir = Path.Combine(outputDirectory, ScreenshotsFolderName);
            if (Directory.Exists(screenshotsDir))
            {
                foreach (var staleFile in Directory.EnumerateFiles(screenshotsDir))
                    File.Delete(staleFile);
            }
            else
            {
                Directory.CreateDirectory(screenshotsDir);
            }
            return screenshotsDir;
        }

        private static void SaveScreenshot(ProjectScreenshot screenshot, string screenshotsDir)
        {
            File.WriteAllBytes(Path.Combine(screenshotsDir, screenshot.Name), screenshot.Data);

            _jsonBuilder?.BeginObject();
            _jsonBuilder?.AddStrPair("Name", screenshot.Name);
            _jsonBuilder?.AddStrPair("StoragePath", screenshot.StoragePath);
            _jsonBuilder?.AddBoolPair("IsProjectPreview", screenshot.IsProjectPreview);
            // Path relative to the json — the viewer requests the file by it via /api/screenshot.
            _jsonBuilder?.AddStrPair("File", ScreenshotsFolderName + "/" + screenshot.Name);
            _jsonBuilder?.EndObject();
        }
    }
}
