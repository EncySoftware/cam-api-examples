using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

#nullable disable
/// <summary>
/// Represents a collection of parameters used within the PLM extension.
/// </summary>
public class PLMParameters : IPLMParameters
{
    /// <summary>
    /// Gets or sets the connection parameters for the PLM extension.
    /// </summary>
    public IPLMConnectionParameters Connection { get; set; }

    /// <summary>
    /// Gets or sets the login parameters for user authentication.
    /// </summary>
    public IPLMLoginParameters Login { get; set; }

    /// <summary>
    /// Gets or sets the settings parameters for configuring the PLM extension.
    /// </summary>
    public IPLMSettingsParameters Settings { get; set; }

    /// <summary>
    /// Gets or sets the project preview information.
    /// </summary>
    public IPLMProjectPreview ProjectPreview { get; set; }

    private Dictionary<string, string> paramValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMParameters"/> class.
    /// </summary>
    public PLMParameters()
    {
        InitParameters();        
        ProjectPreview = new PLMProjectPreview();
        InitParameterValues();
    }

    /// <summary>
    /// Sets parameter values for the PLM extension.
    /// </summary>
    /// <param name="values">The parameter values to be set.</param>
    public void SetParameterValues(IPLMParameterValues values)
    {
        for (int i = 0; i < values.Count; i++)
            if(paramValues.ContainsKey(values[i].Id))
                paramValues[values[i].Id] = values[i].Value;
    }

    /// <summary>
    /// Gets the parameter value associated with the specified key.
    /// </summary>
    /// <param name="key">The key identifying the parameter.</param>
    /// <returns>The parameter value as a string.</returns>
    public string this[string key] => GetParameterValue(key);

    private void InitParameters()
    {
        var connectionParams = new List<PLMConnectionParameter>
        {
            new() {
                Id = "Host",
                Name = "Host",
                DefaultValue = "myhost",
                Order = 0
            },
            new() {
                Id = "Port",
                Name = "Port",
                DefaultValue = "7001",
                Order = 1
            }
        };

        var loginParams = new List<PLMLoginParameter>
        {
            new() {
                Id = "Login",
                Name = "Login",
                Mandatory = true,
                Password = false,
                Login = true,
                DefaultValue = string.Empty,
                Order = 0
            },
            new() {
                Id = "Password",
                Name = "Password",
                Mandatory = true,
                Password = true,
                Login = false,
                DefaultValue = string.Empty,
                Order = 1
            }
        };

        var settingsParams = new List<PLMSettingsParameter>
        {
            new() {
                Id = "PLMFolder",
                Name = "PLM Folder",
                DefaultValue = string.Empty,
                Order = 0
            }
        };

        Connection = new PLMConnectionParameters(connectionParams);
        Login = new PLMLoginParameters(loginParams, null);
        Settings = new PLMSettingsParameters(settingsParams);
    }

    private void InitParameterValues()
    {
        paramValues = [];

        for (int i = 0; i < Connection.Count; i++)
            paramValues.Add(Connection[i].Id, Connection[i].DefaultValue);

        for (int i = 0; i < Login.Count; i++)
            paramValues.Add(Login[i].Id, Login[i].DefaultValue);

        for (int i = 0; i < Settings.Count; i++)
            paramValues.Add(Settings[i].Id, Settings[i].DefaultValue);
    }

    private string GetParameterValue(string key)
    {
        if (paramValues.ContainsKey(key))
            return paramValues[key];
        else return string.Empty;
    }
}
