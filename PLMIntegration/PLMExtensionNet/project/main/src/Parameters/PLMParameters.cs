using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

public class PLMParameters : IPLMParameters
{
    public IPLMConnectionParameters Connection { get; set; }

    public IPLMLoginParameters Login { get; set; }

    public IPLMSettingsParameters Settings { get; set; }

    public IPLMProjectPreview ProjectPreview { get; set; }

    public PLMParameters(List<TempParam> connectionParams, List<TempLoginParam> loginParams, List<TempParamValue>? loginParamValues, List<TempParam> settingsParams)
    {
        var connection = new PLMConnectionParameters();
        connection.Load(connectionParams);
        Connection = connection;

        var login = new PLMLoginParameters();
        login.Load(loginParams, loginParamValues);
        Login = login;

        var settings = new PLMSettingsParameters();
        settings.Load(settingsParams);
        Settings = settings;
        
        ProjectPreview = new PLMProjectPreview();
    }
}
