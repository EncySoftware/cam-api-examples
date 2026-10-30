using System.Diagnostics;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Logger;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// NC files exporter with one target: it puts the generated program on the clipboard and opens
/// the web editor in the default browser
/// </summary>
public class ExtensionOpenInWebEditor : IExtension, IExtensionNCFilesExporter
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Number of export targets provided by this extension
    /// </summary>
    public int TargetsCount => 1;

    /// <summary>
    /// Caption of a target by its index (shown in UI)
    /// </summary>
    public string GetTargetCaption(int targetIndex)
    {
        return targetIndex == 0 ? WebEditorSettings.Caption : string.Empty;
    }

    /// <summary>
    /// Put the main file of the generated program on the clipboard and open the editor page
    /// </summary>
    public void ExportToTarget(int targetIndex, ICamApiNCGenerationResults ncGenerationResults, out TResultStatus ret)
    {
        ret = default;
        try
        {
            var ncFiles = ReadNcFileNames(ncGenerationResults);
            var programFile = MainProgramFile.Pick(ncFiles, WebEditorSettings.MainFileExtension)
                              ?? throw new Exception("The postprocessor produced no NC files");

            var text = NcFileReader.ReadText(programFile);
            if (text.Length == 0)
                throw new Exception("The NC file is empty: " + programFile);

            ClipboardText.Set(text);
            OpenInBrowser(WebEditorSettings.Url);

            UIDialogs.Notify(TLogEventType.leInfo, WebEditorSettings.Caption, BuildHint(programFile, ncFiles));
        }
        catch (Exception e)
        {
            ret.Code = TResultStatusCode.rsError;
            ret.Description = e.Message;
        }
    }

    private static List<string> ReadNcFileNames(ICamApiNCGenerationResults ncGenerationResults)
    {
        using var resultsCom = ComWrapper.Create(ncGenerationResults);
        using var fileNamesCom = resultsCom.InvokeAndWrap(results => results.NCFileNames);
        if (fileNamesCom.IsNull)
            return [];
        return fileNamesCom.AsEnumerable()
            .Where(file => !string.IsNullOrWhiteSpace(file))
            .ToList();
    }

    private static void OpenInBrowser(string url)
    {
        using var browser = Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
    }

    private static string BuildHint(string programFile, IReadOnlyList<string> ncFiles)
    {
        var hint = $"{Path.GetFileName(programFile)} is on the clipboard. {WebEditorSettings.PasteHint}";

        var notSent = ncFiles.Where(file => file != programFile).Select(Path.GetFileName).ToList();
        if (notSent.Count > 0)
            hint += $" Not sent: {string.Join(", ", notSent)}.";
        return hint;
    }
}
