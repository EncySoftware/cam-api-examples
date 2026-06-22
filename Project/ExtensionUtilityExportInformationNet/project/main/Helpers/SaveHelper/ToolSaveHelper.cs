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
                    _jsonBuilder.AddStrPair("ToolName", toolCom.ToolName());
                    WriteAssemblyItems(toolCom.AssemblyItemsJSON());
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
    }    
}