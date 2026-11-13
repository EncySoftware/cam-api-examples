using System.Diagnostics;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.Logger;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionNCOpenInWebEditorNet;

/// <summary>
/// NC files exporter with one target: it opens the web editor in the default browser with the generated
/// program in the link
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
    /// Put the files of the generated program into the editor link and open it in the browser
    /// </summary>
    public void ExportToTarget(int targetIndex, ICamApiNCGenerationResults ncGenerationResults, out TResultStatus ret)
    {
        ret = default;
        try
        {
            var ncFiles = ReadNcFileNames(ncGenerationResults);
            var program = ProgramFiles.Pick(ncFiles, WebEditorSettings.FileKeys, WebEditorSettings.MainFileKey)
                          ?? throw new Exception(NoProgramMessage(ncFiles));

            var parts = program.Sent.Select(file => new EditorLinkPart(file.Key, File.ReadAllBytes(file.Path))).ToList();
            if (parts[0].Content.Length == 0)
                throw new Exception("The NC file is empty: " + program.Sent[0].Path);

            var link = EditorLink.Build(WebEditorSettings.Url, program.Name, parts);
            OpenInBrowser(BrowserTarget.For(link, program.Name));

            UIDialogs.Notify(TLogEventType.leInfo, WebEditorSettings.Caption, BuildHint(program));
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

    private static string NoProgramMessage(IReadOnlyList<string> ncFiles)
    {
        if (ncFiles.Count == 0)
            return "The postprocessor produced no NC files";
        return $"The editor opens files with the extensions {string.Join(", ", WebEditorSettings.FileKeys)}. " +
               $"The postprocessor produced: {string.Join(", ", ncFiles.Select(Path.GetFileName))}";
    }

    private static void OpenInBrowser(string target)
    {
        using var browser = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    private static string BuildHint(ProgramFiles program)
    {
        var hint = $"Opened in the editor: {string.Join(", ", program.Sent.Select(file => Path.GetFileName(file.Path)))}.";
        if (program.NotSent.Count > 0)
            hint += $" Not sent: {string.Join(", ", program.NotSent.Select(Path.GetFileName))}.";
        return hint;
    }
}
