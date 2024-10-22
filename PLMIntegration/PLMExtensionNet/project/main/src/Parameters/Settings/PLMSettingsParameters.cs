using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

public class PLMSettingsParameters : IPLMSettingsParameters
{
    public int Count => settingsParameters.Count;


    public IPLMSettingsParameter this[int Index] => settingsParameters[Index];

    public PLMSettingsParameters()
    {
        settingsParameters = new List<IPLMSettingsParameter>();
    }

    private List<IPLMSettingsParameter> settingsParameters;

    public void Load(IEnumerable<TempParam> settingsParams)
    {
        foreach (var param in settingsParams)
            settingsParameters.Add(new PLMSettingsParameter {
                Id = param.Id,
                Name = param.Name,
                DefaultValue = param.DefaultValue,
                Order = param.Order
            });
    }
}
