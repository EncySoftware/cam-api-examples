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
            string folderPath = Path.Combine("project","main","OperationToolpathsJSON", subFolder);
            
            var opSetupStageIndex = TechOperationHelper.SetupStageIndex(operationCom);
            var opPartIndex = TechOperationHelper.PartIndex(operationCom);
            var opType = TechOperationHelper.OperationType(operationCom);
            string fileName = 
                $"I_{opSetupStageIndex}_{opPartIndex}_{opType}.json";
            string fullPath = Path.Combine(folderPath, fileName);
            
            Directory.CreateDirectory(folderPath);
            File.WriteAllText(fullPath, json);

            _jsonBuilder?.AddStrPair("ToolpathFileName", fullPath); 
        }
    }
}