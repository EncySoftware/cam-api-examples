using CAMAPI.Technologist;

namespace ExtensionUtilityNcMakerNet;

/// <summary>
/// Filter by name of operation (should contain substring)
/// </summary>
public class OperationsFilterByName: ICamApiTechOperationIteratorFilter
{
    private readonly string _subString;

    /// <summary>
    /// Filter by name of operation (should contain substring)
    /// </summary>
    public OperationsFilterByName(string subString) {
        _subString = subString;
    }

    /// <inheritdoc />
    public bool AllowIterateTo(ICamApiTechOperation operation)
    {
        var allow = string.IsNullOrEmpty(_subString);
        if (!allow)
            allow = operation.IsGroup || operation.FullName.Contains(_subString, StringComparison.InvariantCultureIgnoreCase);
        return allow;
    }
}