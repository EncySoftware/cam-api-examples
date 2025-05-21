using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Parameters;

/// <summary>
/// Represents login parameters used for user authentication in the PLM extension.
/// </summary>
public class PLMLoginParameters : IPLMLoginParameters
{
    /// <summary>
    /// Gets the number of parameters in the collection.
    /// </summary>
    public int Count => loginParameters.Count;

    /// <summary>
    /// Gets the parameter at the specified index.
    /// </summary>
    /// <param name="index">The index of the parameter.</param>
    /// <returns>The parameter at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the index is out of range.
    /// </exception>
    public IPLMLoginParameter this[int index] => loginParameters[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMLoginParameters"/> class.
    /// </summary>
    public PLMLoginParameters(IEnumerable<IPLMLoginParameter> loginParams, IEnumerable<IPLMLoginParamValue>? paramValues)
    {
        loginParameters = loginParams.ToList();
        var localeparam = loginParameters.FirstOrDefault(lp => lp.Id == "Locale");
        if (!(localeparam is null || paramValues is null))
        {
            var listOfValues = new PLMLoginParamListOfValues(paramValues);
            ((PLMLoginParameter)localeparam).LOV = listOfValues;
        }
    }

    private List<IPLMLoginParameter> loginParameters;
}
