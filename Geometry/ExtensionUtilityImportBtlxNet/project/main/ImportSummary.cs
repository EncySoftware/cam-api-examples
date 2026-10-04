using System.Text;

namespace ExtensionUtilityImportBtlxNet;

/// <summary>
/// What the import of a BTLx file put into the project
/// </summary>
/// <param name="PartsCount">Number of parts put into the geometry tree</param>
/// <param name="GroupName">Name of the geometry tree group the parts lie in</param>
public record ImportSummary(int PartsCount, string GroupName)
{
    /// <summary>
    /// Text of the summary to show to the user
    /// </summary>
    public string BuildReport()
    {
        var report = new StringBuilder();
        report.AppendLine($"Parts imported: {PartsCount}");
        report.AppendLine($"Geometry group: {GroupName}");
        return report.ToString();
    }
}
