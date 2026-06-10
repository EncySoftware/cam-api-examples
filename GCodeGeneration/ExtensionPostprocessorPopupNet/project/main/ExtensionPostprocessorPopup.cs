using System.Diagnostics;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.NCMaker;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;

namespace ExtensionPostprocessorPopupNet;

/// <summary>
/// Extension to demonstrate entry point "postprocessor_popup"
/// </summary>
public class ExtensionPostprocessorPopup : IExtension, IExtensionPostprocessorPopup
{
    private static readonly string[] ItemCaptions =
    [
        "Show postprocessor info",
        "Show postprocessor info 1",
        "Show postprocessor info 2"
    ];

    private static readonly string[] PostProcessorFiles =
    [
        "Sinumerik (840D)_Mill.sppx",
        "Heidenhain (iTNC530)_Mill.sppx",
        "Fanuc (30i)_Mill.sppx"
    ];

    private static readonly string[] NcFileNames =
    [
        "sinumerik840d.nc",
        "heidenhain_itnc530.nc",
        "fanuc30i.nc"
    ];

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public int ItemsCount => ItemCaptions.Length;

    /// <inheritdoc />
    public string GetItemCaption(int itemIndex) => ItemCaptions[itemIndex];

    /// <inheritdoc />
    public void ExecuteItem(int itemIndex, ICamApiProject activeProject, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            if (activeProject == null)
                throw new Exception("No active project");

            using var pathsHelper = SystemExtensionFactory.GetPathsHelper();
            using var projectCom = new ComWrapper<ICamApiProject>(activeProject);
            using var technologistCom = projectCom.Technologist();
            using var ncMakerCom = projectCom.NCMaker();
            using var operationCom = technologistCom.GetOperations(TCamApiReorderingMode.rmReordered);

            // Limit set of operations by substring inside full name
            // operationCom.Invoke(it => it.OperationsFilter = new OperationsFilterByName("Setup stage 1"));
            
            // make CLData
            var clDataFile = Path.Combine(Path.GetTempPath(),
                Path.GetFileNameWithoutExtension(NcFileNames[itemIndex]) + ".inpcld");
            projectCom.SaveClData(clDataFile, operationCom);

            // make settings for CNC generating
            using var settingsCom = ncMakerCom.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx);
            var resultNcCodeFile = settingsCom.Invoke(s =>
            {
                var sppx = (ICamApiMakeCncSppxSettings)s;
                sppx.OutputFolder = Path.GetTempPath();
                sppx.NcFileName = NcFileNames[itemIndex];
                return Path.Combine(sppx.OutputFolder, sppx.NcFileName);
            });

            // get postprocessor from all users documents folder
            var postProcessor = Path.Combine(pathsHelper.PostprocessorsFolder(), "Mill", PostProcessorFiles[itemIndex]);
            if (!File.Exists(postProcessor))
                throw new Exception("Postprocessor not found: " + postProcessor);

            // generate CNC
            using var generatedFiles = ncMakerCom.Generate(clDataFile, postProcessor, settingsCom);
            Process.Start("notepad.exe", resultNcCodeFile);
        }
        catch (Exception ex)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = ex.Message;
        }
    }
}
