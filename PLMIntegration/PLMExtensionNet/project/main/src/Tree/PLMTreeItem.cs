using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Tree;

/// <summary>
/// Represents an item in a hierarchical PLM tree structure.
/// </summary>
public class PLMTreeItem : IPLMTreeItem
{
    /// <summary>
    /// Gets or sets the unique identifier of the tree item.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the tree item.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the tree item.
    /// </summary>
    public TPLMItemType Type { get; set; }

    /// <summary>
    /// Gets or sets the comment associated with the tree item.
    /// </summary>
    public string Comment { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether child items have been loaded.
    /// </summary>
    public bool ChildsLoaded { get; set; }

    /// <summary>
    /// Gets or sets the collection of child items.
    /// </summary>
    public IPLMTree? Childs { get; set; }

}
