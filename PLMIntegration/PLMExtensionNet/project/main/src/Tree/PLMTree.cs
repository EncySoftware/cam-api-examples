using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Tree;

/// <summary>
/// Represents hierarchical tree of items within the PLM extension.
/// </summary>
public class PLMTree : IPLMTree
{
    /// <summary>
    /// Gets the number of tree items in the collection.
    /// </summary>
    public int Count => treeItems.Count();

    /// <summary>
    /// Gets the tree item at the specified index.
    /// </summary>
    /// <param name="index">The index of the tree item.</param>
    /// <returns>The <see cref="IPLMTreeItem"/> at the specified index.</returns>
    public IPLMTreeItem this[int index] => treeItems[index];

    private List<PLMTreeItem> treeItems;

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMTree"/> class.
    /// </summary>
    public PLMTree()
    {
        treeItems = [];
    }

    /// <summary>
    /// Adds a new tree item to the collection.
    /// </summary>
    /// <param name="id">The unique identifier of the tree item.</param>
    /// <param name="name">The name of the tree item.</param>
    /// <param name="comment">A comment associated with the tree item.</param>
    /// <param name="itemType">The type of the tree item.</param>
    public void AddTreeItem(string id, string name, string comment, TPLMItemType itemType) => treeItems.Add(new PLMTreeItem
    {
        Id = id,
        Name = name,
        Type = itemType,
        Comment = comment
    });
}
