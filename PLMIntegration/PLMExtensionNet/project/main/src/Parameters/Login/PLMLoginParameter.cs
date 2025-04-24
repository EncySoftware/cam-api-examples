using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents a login parameter used for authentication within the PLM extension.
/// </summary>
public class PLMLoginParameter : IPLMLoginParameter
{
    /// <summary>
    /// Gets or sets the unique identifier of the login parameter.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the login parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default value of the login parameter.
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order in which the login parameter appears.
    /// </summary>

    public int Order { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the login parameter is mandatory.
    /// </summary>
    public bool Mandatory { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this parameter is a password field.
    /// </summary>
    public bool Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this parameter represents a login field.
    /// </summary>
    public bool Login { get; set; }

    /// <summary>
    /// Gets or sets the list of possible values for the login parameter.
    /// </summary>
    public IPLMLoginParamListOfValues? LOV { get; set; } 
}
