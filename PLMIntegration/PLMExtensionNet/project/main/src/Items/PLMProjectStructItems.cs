using CAMAPI.Extension.PLM;

namespace PLMIntegrarionExamples.Items;

/// <summary>
/// Represents a collection of project structure items within the PLM extension.
/// </summary>
public class PLMProjectStructItems : IPLMProjectStructItems
{
    /// <summary>
    /// Gets the number of project structure items in the collection.
    /// </summary>
    public int Count => projectStructItems.Count;

    /// <summary>
    /// Gets the project structure item at the specified index.
    /// </summary>
    /// <param name="index">The index of the project structure item.</param>
    /// <returns>The project structure item at the specified index.</returns>
    /// <exception cref="IndexOutOfRangeException">
    /// Thrown if the index is out of range.
    /// </exception>
    public IPLMProjectStructItem this[int index] => projectStructItems[index];

    /// <summary>
    /// Initializes a new instance of the <see cref="PLMProjectStructItems"/> class.
    /// </summary>
    public PLMProjectStructItems()
    {
        projectStructItems = new List<IPLMProjectStructItem>();
    }

    private List<IPLMProjectStructItem> projectStructItems;
}
