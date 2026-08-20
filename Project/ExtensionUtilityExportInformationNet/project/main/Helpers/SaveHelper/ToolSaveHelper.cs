using System.Text.Json;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.Tools;
using CAMAPI.ToolsList;

namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A helper class for saving project data.
    /// </summary>
    public static class ToolSaveHelper
    {
        private static JsonBuilder? _jsonBuilder;

        /// <summary>
        /// Initializes the JSON builder for saving project data.
        /// </summary>
        public static void Initialize(JsonBuilder builder)
        {
            _jsonBuilder = builder ?? throw new ArgumentNullException(nameof(builder));
        }

        /// <summary>
        /// Saves the project data to the JSON builder.
        /// </summary>
        public static void SaveToolDetails(ComWrapper<ICamApiProject> projectCom)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            using var toolsList = projectCom.ToolsList();
            int toolsCnt = toolsList.Count();

            _jsonBuilder.BeginArray("ToolsList");
            for (int i = 0; i < toolsCnt; i++)
            {
                using var toolInfoCom = toolsList.ToolInfo(i);
                using var toolCom = toolInfoCom.ToolEntity();

                _jsonBuilder.BeginObject(); // tool
                if (!toolCom.IsNull)
                {
                    string toolId = toolInfoCom.ToolID();
                    _jsonBuilder.AddStrPair("ToolName", toolCom.ToolName());
                    //_jsonBuilder.AddStrPair("ToolNotes", toolCom.Notes());
                    _jsonBuilder.AddStrPair("ToolID", toolId);
                    _jsonBuilder.AddStrPair("ToolGUID", toolInfoCom.ToolGUID());
                    _jsonBuilder.AddStrPair("ToolCaption", toolInfoCom.ToolCaption());
                    _jsonBuilder.AddIntPair("ToolNumber", toolInfoCom.ToolNumber());
                    _jsonBuilder.AddIntPair("MagazineNumber", toolInfoCom.MagazineNumber());
                    WriteAssemblyItems(toolCom.AssemblyItemsJSON());
                    WriteOperationsUsingTool(toolsList, toolId);
                }
                _jsonBuilder.EndObject(); // tool closing
            }
            _jsonBuilder.EndArray(); // ToolsList closing
        }

        /// <summary>
        /// Parses the assembly JSON ({ AssemblyName, AssemblyItems:[{ItemType,ItemName}] })
        /// and writes its items structurally into the JSON builder.
        /// </summary>
        private static void WriteAssemblyItems(string assemblyJson)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            _jsonBuilder.BeginArray("AssemblyItems");
            if (!string.IsNullOrEmpty(assemblyJson))
            {
                using var doc = JsonDocument.Parse(assemblyJson);
                if (doc.RootElement.TryGetProperty("AssemblyItems", out var items)
                    && items.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in items.EnumerateArray())
                    {
                        _jsonBuilder.BeginObject();
                        _jsonBuilder.AddStrPair("ItemType", item.GetProperty("ItemType").GetString() ?? "");
                        _jsonBuilder.AddStrPair("ItemName", item.GetProperty("ItemName").GetString() ?? "");
                        _jsonBuilder.EndObject();
                    }
                }
            }
            _jsonBuilder.EndArray(); // AssemblyItems closing
        }

        /// <summary>
        /// Writes the "UsedInOperations" array: operations that use the given tool
        /// (link for the viewer to show tool->operations and operation->tool).
        /// </summary>
        private static void WriteOperationsUsingTool(
            ComWrapper<ICamApiMachiningToolsList> toolsList, string toolId)
        {
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");

            _jsonBuilder.BeginArray("UsedInOperations");
            using var operationsIterator = toolsList.GetOperationsUsingTheTool(toolId);
            if (!operationsIterator.IsNull)
            {
                operationsIterator.Reset();
                do
                {
                    if (operationsIterator.CurrentOperationIsEmpty())
                        continue;
                    _jsonBuilder.BeginObject();
                    _jsonBuilder.AddStrPair("OperationID", operationsIterator.GetCurrentOperationID());
                    _jsonBuilder.AddStrPair("OperationCaption", operationsIterator.GetCurrentOperationCaption());
                    _jsonBuilder.EndObject();
                }
                while (operationsIterator.MoveNext());
            }
            _jsonBuilder.EndArray(); // UsedInOperations closing
        }
    }    
}