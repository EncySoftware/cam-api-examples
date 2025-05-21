using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

/// <summary>
/// Represents a data item within the PLM extension.
/// </summary>
public class PLMDataItem : IPLMDataItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the data item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the data item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the data item.
    /// </summary>
    public TPLMItemType Type { get; set; }

    /// <summary>
    /// Gets or sets the last modification time of the data item.
    /// </summary>
    public double TimeStamp { get; set; }

    /// <summary>
    /// Gets or sets the collection of files associated with the data item.
    /// </summary>
    public IPLMFiles Files { get => plmFiles; }

    /// <summary>
    /// Gets or sets the attributes of the data item.
    /// </summary>
    public IPLMItemAttributes? Attributes { get; set; }

    private PLMFiles plmFiles = new PLMFiles();

    /// <summary>
    /// Adds a file to the data item.
    /// </summary>
    /// <param name="filePath">The file path of the file to be added.</param>
    public void AddFile(string filePath) => plmFiles.AddFile(filePath);

    /// <summary>
    /// Adds multiple files to the data item.
    /// </summary>
    /// <param name="filePaths">A collection of file paths to be added.</param>
    public void AddFiles(IEnumerable<string> filePaths) => plmFiles.AddFiles(filePaths);
}
