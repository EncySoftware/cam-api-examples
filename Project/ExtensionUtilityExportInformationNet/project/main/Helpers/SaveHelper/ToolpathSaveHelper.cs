using CAMAPI.DotnetHelper;
using CAMAPI.Machine;
using CAMAPI.TechOperation;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A helper class for saving operation`s toolpath data.
    /// </summary>
    public static class ToolpathSaveHelper
    {
        private static JsonBuilder? _jsonBuilder;

        /// <summary>
        /// Index for operation ordering
        /// </summary> 
        private static int _operationIndex;

        /// <summary>
        /// Reseting index for operation ordering (for example, for different modes).
        /// </summary>
        public static void ResetOperationIndex()
        {
            _operationIndex = 0;
        }

        /// <summary>
        /// Initializes the JSON builder for saving part data.
        /// </summary>
        public static void Initialize(JsonBuilder builder)
        {
            _jsonBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Saves the toolpath of a given operation to a JSON file.
        /// </summary>
        public static void SaveOperationToolpath(
            ComWrapper<ICamApiTechOperation> operationCom, TCamApiReorderingMode mode)
        {
            
            if (!TechOperationHelper.HasToolpath(operationCom)){ // if operation has no toolpath no need of creating empty file
                _jsonBuilder?.AddStrPair("ToolpathFileName", "Null");
                return;
            }  

            var jsonToolPathbuilder = new JsonBuilder();

            jsonToolPathbuilder.BeginObject();
            jsonToolPathbuilder.BeginObject("CAMToolpath");
            jsonToolPathbuilder.BeginArray("Commands");

            var exportToolpathReceiver = new ExportToolpathReceiver(jsonToolPathbuilder, treeOutput: false);
            TechOperationHelper.ExportToolpath(operationCom, exportToolpathReceiver);

            jsonToolPathbuilder.EndArray();
            jsonToolPathbuilder.EndObject(); // CAMToolpath closing
            jsonToolPathbuilder.EndObject();

            string json = jsonToolPathbuilder.GetJsonString(pretty: true);

            string subFolder = (mode == TCamApiReorderingMode.rmDesigned) ? "Designed" : "Reordered";
            string relativeFolder = Path.Combine("project", "main", "OperationToolpathsJSON", subFolder);
            string folderPath = Path.Combine(ExportOutputPaths.Root, relativeFolder);
            
            var opSetupStageIndex = TechOperationHelper.SetupStageIndex(operationCom);
            var opPartIndex = TechOperationHelper.PartIndex(operationCom);
            var opFullName = SanitizeFileName(TechOperationHelper.FullName(operationCom));
            var opType = TechOperationHelper.OperationType(operationCom);
            string fileName = 
                $"I_{opSetupStageIndex}_{opPartIndex}_{_operationIndex++}_{opType}_{opFullName}.json";
            
            Directory.CreateDirectory(folderPath);
            File.WriteAllText(Path.Combine(folderPath, fileName), json);

            _jsonBuilder?.AddStrPair("ToolpathFileName", Path.Combine(relativeFolder, fileName)); 
        }

        private static string SanitizeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            return string.Concat(name.Select(c => invalid.Contains(c) || c == ' ' ? '_' : c));
        }
    }
}