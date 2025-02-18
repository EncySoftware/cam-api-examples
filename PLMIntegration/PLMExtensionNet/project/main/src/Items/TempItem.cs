using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

public class TempItem
{    
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TPLMItemType ItemType { get; set; }

    public Double TimeStamp { get; set; } = 0;

    public List<string> FilePaths { get; set; } = new List<string>();
}
