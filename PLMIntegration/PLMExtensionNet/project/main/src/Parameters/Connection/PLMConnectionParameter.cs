using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

public class PLMConnectionParameter : IPLMConnectionParameter
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DefaultValue { get; set; } = string.Empty;

    public int Order { get; set; }
}
