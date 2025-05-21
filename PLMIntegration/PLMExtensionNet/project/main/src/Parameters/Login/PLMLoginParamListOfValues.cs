using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents the list of possible values for the login parameter in the PLM extension.
/// </summary>
public class PLMLoginParamListOfValues : IPLMLoginParamListOfValues
{
    /// <summary>
    /// Gets the number of possible values for the login parameter in the collection.
    /// </summary>
    public int Count => loginParamValues.Count;

    /// <summary>
    /// Gets the possible value for the login parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter.</param>
    /// <returns>The parameter at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the index is out of range.
    /// </exception>
    public IPLMLoginParamValue this[int index] => loginParamValues[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMLoginParamListOfValues"/> class.
    /// </summary>
    public PLMLoginParamListOfValues(IEnumerable<IPLMLoginParamValue> paramValues)
    {
        loginParamValues = paramValues.ToList();
    }

    private List<IPLMLoginParamValue> loginParamValues;  
}
