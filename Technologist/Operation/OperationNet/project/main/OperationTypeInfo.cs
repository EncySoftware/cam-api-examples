namespace ExtensionOperationsNet
{
    /// <summary>
    /// A class for operation parameters
    /// </summary>
    public partial class OperationTypeInfo
    {
        /// <summary>
        /// Parameter for operation`s Id
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Parameter for operation`s caption
        /// </summary> 
        public string Caption { get; set; } = string.Empty;
        /// <summary>
        /// Parameter for operation`s XMLFilePath
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Default constructor
        /// </summary>
        public OperationTypeInfo(string id, string caption, string filePath)
        {
            Id = id;
            Caption = caption;
            FilePath = filePath;
        }
    }
};