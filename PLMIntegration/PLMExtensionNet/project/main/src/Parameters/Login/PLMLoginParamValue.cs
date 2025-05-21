using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents the possible value for the login parameter in the PLM extension.
/// </summary>
public class PLMLoginParamValue : IPLMLoginParamValue
{
    /// <summary>
    /// Gets or sets the possible login value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the possible login value.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;   
}
