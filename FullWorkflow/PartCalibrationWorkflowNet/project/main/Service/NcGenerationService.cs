using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Tab 2 demo helper: calculate the project's toolpaths and postprocess them
/// into an NC program, then open it — this is the program you would hand to the
/// machine. The whole COM sequence (technologist, operations, CLData, NCMaker,
/// settings) runs inside ONE worker-thread Invoke so every object stays on the
/// same apartment.
/// </summary>
internal static class NcGenerationService
{
    /// <summary>
    /// Calculate all operation toolpaths, postprocess to an NC file and open it.
    /// Returns a short status message. Throws with a clear message on failure.
    /// </summary>
    public static string CalculateAndOpenNc(ComWrapper<ICamApiApplication> appCom)
    {
        // Resolve a postprocessor (no COM object crosses threads — just a path).
        string ppFolder;
        using (var pathsCom = SystemExtensionFactory.GetPathsHelper())
            ppFolder = pathsCom.Invoke(p => p.PostprocessorsFolder)
                ?? throw new InvalidOperationException("Cannot resolve the postprocessors folder.");
        var postProcessor = FindPostprocessor(ppFolder);

        var tempDir = Path.Combine(Path.GetTempPath(), "PartCalib_nc_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var clDataFile = Path.Combine(tempDir, "measurement.inpcld");
        const string ncName = "measurement.nc";
        var ncFile = Path.Combine(tempDir, ncName);

        appCom.Invoke(app =>
        {
            var project = app.GetActiveProject(out var s1)
                ?? throw new InvalidOperationException("No active project — open a project first.");
            Check(s1);

            var tech = project.Technologist;
            // Calculate toolpaths (this is the "расчёт"). Reset first and compute
            // links between operations, mirroring the original GenerateNc.
            tech.ResetAllOperationsToolpath();
            tech.CalculateAllOperationsToolpath(true, out var s2);
            Check(s2);

            // CLData (cutter location) for every operation.
            var ops = tech.GetOperations(TCamApiReorderingMode.rmReordered, out var s3);
            Check(s3);
            project.SaveClData(clDataFile, ops, out var s4);
            Check(s4);

            // Postprocess CLData → NC.
            var ncMaker = project.NCMaker;
            var settings = ncMaker.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx, out var s5);
            Check(s5);
            var sppx = (ICamApiMakeCncSppxSettings)settings;
            sppx.OutputFolder = tempDir;
            sppx.NcFileName = ncName;
            ncMaker.Generate(clDataFile, postProcessor, settings, out var s6);
            Check(s6);
        });

        // Open whatever NC file was produced (expected name first, else any .nc).
        var produced = File.Exists(ncFile)
            ? ncFile
            : Directory.EnumerateFiles(tempDir, "*.nc").FirstOrDefault();
        if (produced != null)
            OpenInNotepad(produced);

        return produced != null
            ? $"Toolpath calculated, NC generated → {Path.GetFileName(produced)} (postproc: {Path.GetFileName(postProcessor)})."
            : $"Toolpath calculated, but no NC file was produced (postproc: {Path.GetFileName(postProcessor)}).";
    }

    private static void Check(TResultStatus status)
    {
        if (status.Code == TResultStatusCode.rsError)
            throw new Exception(status.Description);
    }

    private static string FindPostprocessor(string ppFolder)
    {
        // Prefer a common mill postprocessor (as the FullWorkflow3DProject example
        // does); fall back to the first .sppx found anywhere under the folder.
        var preferred = Path.Combine(ppFolder, "Mill", "Fanuc (30i)_Mill.sppx");
        if (File.Exists(preferred))
            return preferred;
        var any = Directory.EnumerateFiles(ppFolder, "*.sppx", SearchOption.AllDirectories).FirstOrDefault();
        return any ?? throw new InvalidOperationException(
            $"No .sppx postprocessor found under '{ppFolder}'.");
    }

    private static void OpenInNotepad(string path)
    {
        try { Process.Start("notepad.exe", path); }
        catch { /* viewing is best-effort */ }
    }
}
