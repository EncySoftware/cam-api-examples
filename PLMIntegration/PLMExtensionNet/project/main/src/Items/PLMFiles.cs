using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

/// <summary>
/// Represents a collection of files within the PLM extension.
/// </summary>
public class PLMFiles : IPLMFiles
{
    /// <summary>
    /// Gets the number of files in the collection.
    /// </summary>
    public int Count => filePaths.Count;

    /// <summary>
    /// Gets the file at the specified index.
    /// </summary>
    /// <param name="index">The index of the file.</param>
    /// <returns>The file at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the index is out of range.
    /// </exception>
    public string this[int index] => filePaths[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMFiles"/> class.
    /// </summary>
    public PLMFiles()
    {
        filePaths = new List<string>();
    }

    private List<string> filePaths;

    /// <summary>
    /// Adds a file to the collection.
    /// </summary>
    /// <param name="filePath">The file path of the file to be added.</param>
    public void AddFile(string filePath) => filePaths.Add(filePath);

    /// <summary>
    /// Adds multiple files to the collection.
    /// </summary>
    /// <param name="newfilePaths">A collection of file paths to be added.</param>
    public void AddFiles(IEnumerable<string> newfilePaths) => filePaths.AddRange(newfilePaths);
}
