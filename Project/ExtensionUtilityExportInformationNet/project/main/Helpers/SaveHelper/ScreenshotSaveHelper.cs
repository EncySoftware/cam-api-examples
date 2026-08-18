using CAMAPI.DotnetHelper;
using CAMAPI.Project;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// Saves screenshots extracted from the .stcp project file into the json.
    /// </summary>
    public static class ScreenshotSaveHelper
    {
        private static JsonBuilder? _jsonBuilder;

        /// <summary>
        /// Initializes the JSON builder for saving screenshots data.
        /// </summary>
        public static void Initialize(JsonBuilder builder)
        {
            _jsonBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Writes the Screenshots section. Screenshots correspond to the last saved
        /// state of the project, so an unsaved project gets no section. Any extraction
        /// error does not fail the export — ScreenshotsError is written instead.
        /// </summary>
        public static void SaveScreenshots(ComWrapper<ICamApiProject> projectCom)
        {
            var projectFilePath = ProjectHelper.FilePath(projectCom);
            if (string.IsNullOrEmpty(projectFilePath) || !File.Exists(projectFilePath))
                return;

            try
            {
                var screenshots = ProjectScreenshotExtractor.Extract(projectFilePath);
                if (screenshots.Count == 0)
                {
                    _jsonBuilder?.AddStrPair("ScreenshotsError",
                        "No screenshots found: storage has no Thumbnails files (" + projectFilePath + ")");
                    return;
                }

                _jsonBuilder?.BeginArray("Screenshots");
                foreach (var screenshot in screenshots)
                    SaveScreenshot(screenshot);
                _jsonBuilder?.EndArray();
            }
            catch (Exception e)
            {
                _jsonBuilder?.AddStrPair("ScreenshotsError", e.GetType().Name + ": " + e.Message);
            }
        }

        private static void SaveScreenshot(ProjectScreenshot screenshot)
        {
            _jsonBuilder?.BeginObject();
            _jsonBuilder?.AddStrPair("Name", screenshot.Name);
            _jsonBuilder?.AddStrPair("StoragePath", screenshot.StoragePath);
            _jsonBuilder?.AddBoolPair("IsProjectPreview", screenshot.IsProjectPreview);
            _jsonBuilder?.AddStrPair("DataUri", BuildDataUri(screenshot));
            _jsonBuilder?.EndObject();
        }

        private static string BuildDataUri(ProjectScreenshot screenshot)
        {
            return "data:" + MimeTypeByExtension(screenshot.Name) + ";base64,"
                 + Convert.ToBase64String(screenshot.Data);
        }

        private static string MimeTypeByExtension(string fileName)
        {
            return Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".bmp" => "image/bmp",
                ".gif" => "image/gif",
                _ => "image/jpeg",
            };
        }
    }
}
