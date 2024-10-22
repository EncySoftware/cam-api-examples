using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

public class PLMDataItems : IPLMDataItems
{
    public int Count => dataItems.Count;

    public IPLMDataItem this[int Index] => dataItems[Index];

    public PLMDataItems()
    {
        dataItems = new List<IPLMDataItem>();
    }

    public PLMDataItems(List<TempItem> tempItems)
    {
        dataItems = new List<IPLMDataItem>();
        foreach (var item in tempItems)
        {
            var newItem = new PLMDataItem {
                Id = item.Id,
                Name = item.Name,
                Type = item.ItemType,
                TimeStamp = item.TimeStamp
            };

            var itemFiles = new PLMFiles();
            itemFiles.AddFiles(item.FilePaths);
            newItem.Files = itemFiles;

            dataItems.Add(newItem);
        }
    }

    private List<IPLMDataItem> dataItems;
}
