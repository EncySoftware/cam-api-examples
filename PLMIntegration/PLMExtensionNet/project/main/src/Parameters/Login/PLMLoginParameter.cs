using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

public class PLMLoginParameter : IPLMLoginParameter
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string DefaultValue { get; set; } = string.Empty;

    public int Order { get; set; }

    public bool Mandatory { get; set; }

    public bool Password { get; set; }

    public IPLMLoginParamListOfValues? LOV { get; set; } 
}
