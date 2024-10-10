using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

public class PLMProjectStructItem : IPLMProjectStructItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TPLMItemType Type { get; set; }

    public string TimeStamp { get; set; } = string.Empty;

}
