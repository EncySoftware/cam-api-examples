
namespace ExtensionUtilityExportInformationNet
{
    /// <summary>
    /// A helper class for saving part geometry data.
    /// </summary>
    public static class CopyPartSaveHelper
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
        /// Saves the copy part data to the JSON builder.
        /// </summary>
        public static void SaveCopyPartData(PartGeometry partGeometry){
            if (_jsonBuilder == null)
                throw new Exception("Create JSON builder!");
            
            _jsonBuilder.BeginObject("PartGeometry");
            _jsonBuilder.AddStrPair("GeometryType", partGeometry.GeometryType);
            
            _jsonBuilder.AddStrPair("FileName", partGeometry.FileName);
            _jsonBuilder.AddStrPair("SourceCADModelFileID", partGeometry.SourceCADModelFileID);

            _jsonBuilder.BeginArray("ModelItems");
            _jsonBuilder.BeginObject(); // ModelItem 
            _jsonBuilder.AddStrPair("Caption", partGeometry.ModelItems[0].Caption);
            _jsonBuilder.AddStrPair("ModelItemClassName", partGeometry.ModelItems[0].ModelItemClassName);
            _jsonBuilder.EndObject(); // ModelItem closing 
            
            _jsonBuilder.EndArray(); // ModelItems closing  
            _jsonBuilder.BeginObject("GeometryCS");
            _jsonBuilder.AddStrPair("GeometryCSName", partGeometry.GeometryCS.GeometryCSName);
            GeometrySaveHelper.ShowMatrixData(partGeometry.GeometryCS.GeometryCSMatrix, "GeometryCSMatrix", _jsonBuilder);

            _jsonBuilder.EndObject(); // GeometryCS closing
            _jsonBuilder.EndObject(); // PartGeometry closing
        }
    }    
}