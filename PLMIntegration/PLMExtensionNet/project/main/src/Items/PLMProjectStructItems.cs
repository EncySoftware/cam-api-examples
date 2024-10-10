using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

public class PLMProjectStructItems : IPLMProjectStructItems
{
    public int Count => projectStructItems.Count;

    public IPLMProjectStructItem this[int Index] => projectStructItems[Index];

    public PLMProjectStructItems()
    {
        projectStructItems = new List<IPLMProjectStructItem>();
    }

    private List<IPLMProjectStructItem> projectStructItems;
}
