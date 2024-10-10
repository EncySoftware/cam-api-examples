using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

public class TempItem
{    
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public TPLMItemType ItemType { get; set; }

    public string TimeStamp { get; set; } = string.Empty;

    public List<string> FilePaths { get; set; } = new List<string>();
}
