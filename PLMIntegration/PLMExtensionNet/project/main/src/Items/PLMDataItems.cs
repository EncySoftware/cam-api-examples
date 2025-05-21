using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

/// <summary>
/// Represents a collection of data item within the PLM extension.
/// </summary>
public class PLMDataItems : IPLMDataItems
{
    /// <summary>
    /// Gets the number of data items in the collection.
    /// </summary>
    public int Count => dataItems.Count;

    /// <summary>
    /// Gets the data item at the specified index.
    /// </summary>
    /// <param name="index">The index of the data item.</param>
    /// <returns>The data item at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the index is out of range.
    /// </exception>
    public IPLMDataItem this[int index] => dataItems[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMDataItems"/> class.
    /// </summary>
    public PLMDataItems() {}

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMDataItems"/> class.
    /// </summary>
    /// <param name="plmItems">The initial collection of PLM items.</param>
    public PLMDataItems(IPLMItems plmItems)
    {
        for (var i = 0; i < plmItems.Count; i++)
            dataItems.Add(new PLMDataItem {
                Id = plmItems[i].Id,
                Type = plmItems[i].Type
            });
    }
    
    private List<IPLMDataItem> dataItems = [];

    /// <summary>
    /// Adds a new data item to the collection.
    /// </summary>
    /// <param name="itemId">The unique identifier of the data item.</param>
    /// <param name="itemName">The name of the data item.</param>
    /// <param name="itemType">The type of the data item.</param>
    public void AddDataItem(string itemId, string itemName, TPLMItemType itemType) => dataItems.Add(
        new PLMDataItem
        {
            Id = itemId,
            Name = itemName,
            Type = itemType
        });

    /// <summary>
    /// Adds an existing data item to the collection.
    /// </summary>
    /// <param name="dataItem">The <see cref="PLMDataItem"/> instance to be added.</param>
    public void AddDataItem(PLMDataItem dataItem) => dataItems.Add(dataItem);
}
