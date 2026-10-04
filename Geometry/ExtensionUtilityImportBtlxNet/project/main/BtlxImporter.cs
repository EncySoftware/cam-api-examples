using CAMAPI.DotnetHelper;
using CAMAPI.Project;

namespace ExtensionUtilityImportBtlxNet;

/// <summary>
/// Imports a BTLx file into a project: the parts become beams in the geometry tree
/// </summary>
public static class BtlxImporter
{
    /// <summary>
    /// Import the file and return the summary of what was done
    /// </summary>
    public static ImportSummary Import(ComWrapper<ICamApiProject> projectCom, string filePath)
    {
        var parts = BeamGeometryImporter.Place(BtlxReader.Read(filePath));
        if (parts.Count == 0)
            throw new Exception($"The file has no parts: {filePath}");

        var groupName = Path.GetFileNameWithoutExtension(filePath);
        BeamGeometryImporter.Import(projectCom, parts, groupName);

        return new ImportSummary(parts.Count, groupName);
    }
}
