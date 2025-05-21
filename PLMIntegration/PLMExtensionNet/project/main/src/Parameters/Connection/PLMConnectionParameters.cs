using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents connection parameters required for establishing a session with the PLM extension.
/// </summary>
public class PLMConnectionParameters : IPLMConnectionParameters
{
    /// <summary>
    /// Gets the number of parameters in the collection.
    /// </summary>
    public int Count => connectionParameters.Count;

    /// <summary>
    /// Gets the parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter.</param>
    /// <returns>The parameter at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the index is out of range.
    /// </exception>
    public IPLMConnectionParameter this[int index] => connectionParameters[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMConnectionParameters"/> class.
    /// </summary>
    public PLMConnectionParameters(IEnumerable<IPLMConnectionParameter> connectionParams)
    {
        connectionParameters = connectionParams.ToList();
    }

    private List<IPLMConnectionParameter> connectionParameters;  
}
