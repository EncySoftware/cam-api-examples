using System.Diagnostics;
using CAMAPI.NCMaker;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
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

    private void WriteLog(string line) {
        Console.WriteLine(line);
        if (_logFile == null)
            return;
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
            var clDataFile = Path.Combine(tempFolder, "example.inpcld");
            
            using var projectCom = ComWrapper.Create(context.ActiveProject);
            using var ncMakerCom = projectCom.Invoke(project =>
            {
                WriteLog("Active project file: " + project.FilePath);
                WriteLog("Active project ID: " + project.Id);
                return ComWrapper.Create(project.NCMaker);
            });
            
            using var operationCom = ComWrapper.Create(context.SelectedOperation);
            var operation = operationCom.Instance
                ?? throw new Exception("Error getting selected operation");
            
            // make CLData for only current operation
            projectCom.Invoke(project =>
            {
                project.SaveClData(clDataFile, new TechOperationIteratorCurrent(operation), out var status);
                if (status.Code == TResultStatusCode.rsError)
                    throw new Exception("Error saving CLData: " + status.Description);
                WriteLog("CLData saved to file: " + clDataFile);
            });

            // make settings for CNC generating
            string resultGCodeFile = "";
            using var settingsCom = ncMakerCom.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx);
            settingsCom.Invoke(s =>
            {
                var sppx = (ICamApiMakeCncSppxSettings)s;
                sppx.OutputFolder = tempFolder;
                sppx.NcFileName   = "measurement.nc";
                resultGCodeFile = Path.Combine(sppx.OutputFolder, sppx.NcFileName);
            });
            var settings = settingsCom.Instance
                           ?? throw new Exception("Error getting settings");

            // get postprocessor from all users documents folder
            using var pathsHelperCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
            var postProcessor = pathsHelperCom.Invoke(pathsHelper =>
            {
                var result = Path.Combine(pathsHelper.PostprocessorsFolder, "Mill", "Sinumerik (840D)_Mill.sppx");
                if (!File.Exists(result))
                    throw new Exception("Postprocessor not found: " + result);
                WriteLog("Postprocessor found: " + result);
                return result;
            });

            // generate CNC
            ncMakerCom.Generate(clDataFile, postProcessor, settingsCom);
            WriteLog("CNC successfully generated");
            
            
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