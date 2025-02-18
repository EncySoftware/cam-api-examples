using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

public class PLMDataItem : IPLMDataItem
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TPLMItemType Type { get; set; }

    public Double TimeStamp { get; set; } = 0;

    public IPLMFiles? Files { get; set; }

    public IPLMItemAttributes? Attributes { get; set; }
}
