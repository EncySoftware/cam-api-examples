using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

public class PLMLoginParamValue : IPLMLoginParamValue
{
    public string Value { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;   
}
