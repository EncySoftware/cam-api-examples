using System.Diagnostics;
using CAMAPI.Extensions;
using CAMAPI.NCMaker;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.Technologist;
using CAMAPI.TechnologyForm;
using CAMAPI.DotnetHelper;

namespace ExtensionOperationPopupNet;

/// <summary>
/// Make NC for selected operation
/// </summary>
public class MakeNcOnClicked : ICamApiTechnologyFormOperationPopupItemOnClicked
{
    /// <summary>
    /// Path to temporary log file
    /// </summary>
    private string? _logFile;

    /// <summary>
    /// Helper to get paths from CAM instance
    /// </summary>
    private readonly IExtensionInfo? _info;

    /// <summary>
    /// Make NC for selected operation
    /// </summary>
    public MakeNcOnClicked(IExtensionInfo? info)
    {
        _info = info;
    }

    private void WriteLog(string line) {
        Console.WriteLine(line);
        if (_logFile != null)
            File.AppendAllText(_logFile, line + Environment.NewLine);
    }
    
    /// <summary>
    /// Make NC for selected operation
    /// </summary>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        
        try
        {
            // Make temp file name to write log
            var tempFolder = Path.Combine(Path.GetTempPath(), "OperationPopupNcMaker", Path.GetRandomFileName());
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);
            _logFile = Path.Combine(tempFolder, "log.txt");
            
            // context
            using var pathsHelper = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths", _info);
            using var project = new ApiComObject<ICamApiProject>(context.ActiveProject);
            using var operation = new ApiComObject<ICamApiTechOperation>(context.SelectedOperation);
            using var technologist = new ApiComObject<ICamApiTechnologist>(project.Instance.Technologist);
            using var ncMaker = new ApiComObject<ICamApiNCMaker>(project.Instance.NCMaker);

            // show project info
            WriteLog("Active project file: " + project.Instance.FilePath);
            WriteLog("Active project ID: " + project.Instance.Id);

            // make CLData for only current operation
            var clDataFile = Path.Combine(tempFolder, "example.inpcld");
            project.Instance.SaveClData(clDataFile, new TechOperationIteratorCurrent(operation.Instance), out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error saving CLData: " + resultStatus.Description);
            WriteLog("CLData saved to file: " + clDataFile);

            // make settings for CNC generating
            using var settings = new ApiComObject<ICamApiMakeCncSppxSettings>(
                ncMaker.Instance.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx, out resultStatus) as ICamApiMakeCncSppxSettings);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error creating settings: " + resultStatus.Description);
            if (settings == null)
                throw new Exception("Error creating settings: settings is null");
            settings.Instance.OutputFolder = tempFolder;
            settings.Instance.NcFileName = "example.nc";
            var resultGCodeFile = Path.Combine(settings.Instance.OutputFolder, settings.Instance.NcFileName);
            WriteLog("Resulting G code file: " + resultGCodeFile);

            // get postprocessor from all users documents folder
            var postProcessor =
                Path.Combine(pathsHelper.Instance.PostprocessorsFolder, "Mill", "Sinumerik (840D)_Mill.sppx");
            if (!File.Exists(postProcessor))
                throw new Exception("Postprocessor not found: " + postProcessor);
            WriteLog("Postprocessor found: " + postProcessor);

            // generate CNC
            ncMaker.Instance.Generate(clDataFile, postProcessor, settings.Instance, out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error generating CNC: " + resultStatus.Description);
            WriteLog("CNC successfully generated");
            WriteLog(resultStatus.Description);

            // show result
            if (File.Exists(resultGCodeFile))
                Process.Start("notepad.exe", resultGCodeFile);

            resultStatus = default;
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
            WriteLog("Error: " + Environment.NewLine + e.Message);
        }

        if (_logFile != null)
            Process.Start("notepad.exe", _logFile);
    }
}