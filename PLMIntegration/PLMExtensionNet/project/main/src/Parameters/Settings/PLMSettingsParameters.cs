using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents settings parameters for configuring the PLM extension.
/// </summary>
public class PLMSettingsParameters : IPLMSettingsParameters
{
    /// <summary>
    /// Gets the number of parameters in the collection.
    /// </summary>
    public int Count => settingsParameters.Count;

    /// <summary>
    /// Gets the parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter.</param>
    /// <returns>The parameter at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the index is out of range.
    /// </exception>
    public IPLMSettingsParameter this[int index] => settingsParameters[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMSettingsParameters"/> class.
    /// </summary>
    public PLMSettingsParameters(IEnumerable<IPLMSettingsParameter> settingsParams)
    {
        settingsParameters = settingsParams.ToList();
    }

    private List<IPLMSettingsParameter> settingsParameters;
}
