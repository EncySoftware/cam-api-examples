using CAMAPI.DotnetHelper;
using CAMAPI.TechOperation;
using CAMAPI.Technologist;

namespace ToolOrientationFromFacesNet;

/// <summary>
/// Counts the nodes of the machine-side toolpath of every operation, which is what makes the effect
/// of the recalculation visible in the report: the same axes with a stale toolpath give the same
/// node count, a recalculated toolpath gives a different one
/// </summary>
/// <remarks>
/// The MCD tree is only used for reporting. On an SDK that does not expose it this whole file can
/// be dropped without touching the rest of the example.
/// </remarks>
public static class ToolpathReporter
{
    /// <summary>
    /// Number of toolpath nodes of every operation, in the order the operations are designed in
    /// </summary>
    /// <remarks>
    /// Walking the tree leaves interop objects behind that are only released by their finalizer, and
    /// they hold on to a toolpath that any later call may destroy - a finalizer that then runs takes
    /// the process down with an access violation. They are drained here, while the tree they point
    /// at is still alive, rather than before the call that destroys it, by which time it is too late.
    /// </remarks>
    public static List<int> MeasureNodeCounts(ComWrapper<ICamApiTechnologist> technologistCom)
    {
        using var rootOperationCom = technologistCom.RootOperation();
        var rootId = rootOperationCom.Id();

        var counts = new List<int>();
        foreach (var operationCom in technologistCom.EnumerateOperations(TCamApiReorderingMode.rmDesigned))
        {
            if (operationCom.Id() == rootId)
                continue;

            counts.Add(CountNodes(operationCom));
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();

        return counts;
    }

    /// <summary>
    /// Number of toolpath nodes of a single operation
    /// </summary>
    /// <remarks>
    /// The walk moves the iterator instead of asking it for its Current node: counting needs no node
    /// object, and the toolpath nodes are destroyed by the next recalculation, so nothing of them
    /// should outlive this call.
    /// </remarks>
    private static int CountNodes(ComWrapper<ICamApiTechOperation> operationCom)
    {
        using var mcdTreeCom = operationCom.McdTree();

        // groups and operations without a toolpath carry no tree at all
        if (mcdTreeCom.IsNull)
            return 0;

        using var iteratorCom = mcdTreeCom.GetNodes();
        iteratorCom.Reset();

        var count = 0;
        while (true)
        {
            count++;
            if (iteratorCom.MoveToChild())
                continue;

            while (!iteratorCom.MoveToSibling())
            {
                if (!iteratorCom.MoveToParent())
                    return count;
            }
        }
    }
}
