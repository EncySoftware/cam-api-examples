using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents a connection parameter used within the PLM extension.
/// </summary>
public class PLMConnectionParameter : IPLMConnectionParameter
{
    /// <summary>
    /// Gets or sets the unique identifier of the connection parameter.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the connection parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the default value of the connection parameter.
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the order in which the connection parameter appears.
    /// </summary>
    public int Order { get; set; }
}
