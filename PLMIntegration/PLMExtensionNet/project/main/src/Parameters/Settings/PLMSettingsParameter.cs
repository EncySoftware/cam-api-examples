using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents a settings parameter used for configuring the PLM extension.
/// </summary>
public class PLMSettingsParameter : IPLMSettingsParameter
{
    /// <summary>
    /// Gets or sets the unique identifier of the settings parameter.
    /// </summary>
    public string Id {get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the settings parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default value of the settings parameter.
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order in which the settings parameter appears.
    /// </summary>
    public int Order { get; set; }
}