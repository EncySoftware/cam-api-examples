using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

public class PLMSettingsParameter : IPLMSettingsParameter
{
    public string Id
    {
        get => id;
        set => id = value;
    }

    public string Name
    {
        get => name;
        set => name = value;
    }

    public string DefaultValue
    {
        get => defaultValue;
        set => defaultValue = value;
    }


    public int Order
    {
        get => order;
        set => order = value;
    }

    private string id = string.Empty;

    private string name = string.Empty;

    private string defaultValue = string.Empty;

    private int order;
}