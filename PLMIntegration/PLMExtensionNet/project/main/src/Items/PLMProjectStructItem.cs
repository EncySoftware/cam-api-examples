using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

/// <summary>
/// Represents a structure item of project within the PLM extension.
/// </summary>
public class PLMProjectStructItem : IPLMProjectStructItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the project structure item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the project structure item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the project structure item.
    /// </summary>
    public TPLMItemType Type { get; set; }

    /// <summary>
    /// Gets or sets the last modification time of the project structure item.
    /// </summary>
    public double TimeStamp { get; set; }

}
