using System.Diagnostics;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;

namespace ExtensionNCFilesExporterNet;

/// <summary>
/// Example extension implementing IExtensionNCFilesExporter.
/// Demonstrates how to expose NC file export targets from a plugin.
/// </summary>
public class ExtensionNCFilesExporter : IExtension, IExtensionNCFilesExporter
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private static readonly string[] Targets =
    [
        "Export to PLM (demo)",
        "Export to Cloud (demo)",
        "Export to External system (demo)"
    ];

    /// <summary>
    /// Number of export targets provided by this extension
    /// </summary>
    public int TargetsCount => Targets.Length;

    /// <summary>
    /// Caption of a target by its index (shown in UI)
    /// </summary>
    public string GetTargetCaption(int targetIndex)
    {
        if (targetIndex < 0 || targetIndex >= Targets.Length)
            return string.Empty;
        return Targets[targetIndex];
    }

    /// <summary>
    /// Simulated export to the selected target — writes info about the generation
    /// results into a temp file and opens it in notepad.
    /// </summary>
    public void ExportToTarget(int targetIndex, ICamApiNCGenerationResults ncGenerationResults, out TResultStatus ret)
    {
        ret = default;

        try
        {
            var tmpFile = Path.GetTempFileName();
            using (var writer = new StreamWriter(tmpFile))
            {
                writer.WriteLine($"Target: {GetTargetCaption(targetIndex)}");
                writer.WriteLine($"Postprocessor: {ncGenerationResults.PostProcessorFilePath}");
                writer.WriteLine();

                writer.WriteLine("NC files:");
                var ncFiles = ncGenerationResults.NCFileNames;
                if (ncFiles != null)
                {
                    for (var i = 0; i < ncFiles.Count(); i++)
                        writer.WriteLine($"  {ncFiles.Get(i)}");
                }

                writer.WriteLine();
                writer.WriteLine("Messages:");
                var messages = ncGenerationResults.Messages;
                if (messages != null)
                {
                    for (var i = 0; i < messages.Count(); i++)
                        writer.WriteLine($"  {messages.Get(i)}");
                }

                writer.WriteLine();
                writer.WriteLine("Operations:");
                var operations = ncGenerationResults.CLDataInfo?.OperationsList;
                if (operations != null)
                {
                    for (var i = 0; i < operations.Count; i++)
                    {
                        var op = operations.OperationInfo[i];
                        writer.WriteLine($"  {op.Name} ({op.TypeName} {op.ParentOpID} {op.OpID})");
                    }
                }
                else
                {
                    writer.WriteLine("  (not available)");
                }
            }

            Process.Start("notepad.exe", tmpFile);
        }
        catch (Exception ex)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = ex.Message;
        }
    }
}
