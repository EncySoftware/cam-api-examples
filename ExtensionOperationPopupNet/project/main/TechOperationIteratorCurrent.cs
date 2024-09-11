using CAMAPI.Technologist;

namespace ExtensionOperationPopupNet;

/// <summary>
/// Tech operation iterator with only one operation
/// </summary>
public class TechOperationIteratorCurrent : ICamApiTechOperationIterator
{
    /// <summary>
    /// Current operation, which is the only one in the iterator
    /// </summary>
    private ICamApiTechOperation Operation { get; }

    /// <summary>
    /// Filter for operations, but it is ignored in this iterator
    /// </summary>
    public ICamApiTechOperationIteratorFilter? OperationsFilter { get; set; }

    /// <summary>
    /// Tech operation iterator with only one operation
    /// </summary>
    public TechOperationIteratorCurrent(ICamApiTechOperation operation)
    {
        Operation = operation;
    }

    /// <inheritdoc />
    public bool MoveToChild()
    {
        return false;
    }

    /// <inheritdoc />
    public bool MoveToSibling()
    {
        return false;
    }

    /// <inheritdoc />
    public bool MoveToParent()
    {
        return false;
    }

    /// <inheritdoc />
    public ICamApiTechOperation Current()
    {
        return Operation;
    }

    /// <inheritdoc />
    public void Reset()
    {
        // Do nothing
    }
}