using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents a project preview within the PLM extension.
/// </summary>
public class PLMProjectPreview : IPLMProjectPreview
{
    /// <summary>
    /// Gets or sets the width of the project preview.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the project preview.
    /// </summary>
    public int Height { get; set; }
}