using System.Globalization;

namespace AptArcsNet.Model;

/// <summary>What a toolpath node turned out to be</summary>
public enum ToolpathNodeKind
{
    /// <summary>Circular interpolation — the node kept its radius and centre</summary>
    Arc,

    /// <summary>Straight move to a point</summary>
    Line,

    /// <summary>Anything else: header, tool load, feed, comment, tail</summary>
    Other
}

/// <summary>
/// One node of the toolpath as the window shows it. Plain data — no COM references, so it can
/// cross back from the MTA worker to the UI thread.
/// </summary>
/// <param name="Index">Position in the toolpath, 1-based</param>
/// <param name="Kind">What the node turned out to be</param>
/// <param name="Caption">Node text as ENCY shows it in the simulation tree</param>
/// <param name="Radius">Arc radius without sign; null for anything but an arc</param>
public record ToolpathNode(int Index, ToolpathNodeKind Kind, string Caption, double? Radius)
{
    /// <summary>Radius column of the list; empty for everything that is not an arc</summary>
    public string RadiusText => Radius?.ToString("0.###", CultureInfo.InvariantCulture) ?? "";

    /// <summary>Kind column of the list</summary>
    public string KindText => Kind.ToString();

    /// <summary>Arc rows are shown in bold, see the ListViewItem trigger in PluginTheme.xaml</summary>
    public bool IsArc => Kind == ToolpathNodeKind.Arc;
}

/// <summary>
/// Result of one run: every node of the toolpath, plus the arc tally ENCY itself keeps for the
/// operation. The two are counted independently — the nodes by reading the tree, the tally by
/// <c>ICamApiTechOperation.GetBlocksStatistics</c> — and they are expected to agree.
/// </summary>
/// <param name="Nodes">Toolpath nodes in tree order</param>
/// <param name="ReportedArcs">Arcs the operation statistics report</param>
/// <param name="ReportedLines">Straight blocks the operation statistics report, multi-gotos included</param>
public sealed record AptArcsReport(IReadOnlyList<ToolpathNode> Nodes, int ReportedArcs, int ReportedLines)
{
    /// <summary>Arcs found while walking the toolpath tree</summary>
    public int ArcCount => Nodes.Count(n => n.Kind == ToolpathNodeKind.Arc);

    /// <summary>Straight moves found while walking the toolpath tree</summary>
    public int LineCount => Nodes.Count(n => n.Kind == ToolpathNodeKind.Line);

    /// <summary>
    /// Arc radii and how many arcs each one covers, ordered by radius — the sample is expected to
    /// show 4 arcs of R10 and 4 of R20.
    /// </summary>
    public string RadiiSummary()
    {
        var groups = Nodes
            .Where(n => n.Radius.HasValue)
            .GroupBy(n => n.Radius!.Value)
            .OrderBy(g => g.Key)
            .Select(g => $"{g.Count()} x R{g.Key.ToString("0.###", CultureInfo.InvariantCulture)}")
            .ToList();
        return groups.Count == 0 ? "none" : string.Join(", ", groups);
    }
}
