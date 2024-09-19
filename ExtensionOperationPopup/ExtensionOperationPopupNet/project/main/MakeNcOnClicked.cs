using System.Diagnostics;
using System.Runtime.InteropServices;
using CAMAPI.NCMaker;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Technologist;
using CAMAPI.TechnologyForm;

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
        if (_logFile != null)
            File.AppendAllText(_logFile, line + Environment.NewLine);
    }
    
    /// <summary>
    /// Make NC for selected operation
    /// </summary>
    public void OnItemClicked(IExtensionOperationPopupItemOnClickedContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        
        ICamApiMakeCncSppxSettings? settings = null;
        ICamApiNCMaker? ncMaker = null;
        ICamApiTechnologist? technologist = null;
        ICamApiTechOperation? operation = null;
        ICamApiProject? project = null;
        try
        {
            // Make temp file name to write log
            var tempFolder = Path.Combine(Path.GetTempPath(), "OperationPopupNcMaker", Path.GetRandomFileName());
            if (!Directory.Exists(tempFolder))
                Directory.CreateDirectory(tempFolder);
            _logFile = Path.Combine(tempFolder, "log.txt");
            WriteLog("ExtensionNcMaker log started");
            
            project = context.ActiveProject;
            operation = context.SelectedOperation;
            technologist = project.Technologist;
            ncMaker = project.NCMaker;
            
            // show project info
            WriteLog("Active project file: " + project.FilePath);
            WriteLog("Active project ID: " + project.Id);
            
            // make CLData for only current operation
            var clDataFile = Path.Combine(tempFolder, "example.inpcld");
            project.SaveClData(clDataFile, new TechOperationIteratorCurrent(operation), out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error saving CLData: " + resultStatus.Description);
            WriteLog("CLData saved to file: " + clDataFile);
            
            // make settings for CNC generating
            settings = ncMaker.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx, out resultStatus) as ICamApiMakeCncSppxSettings;
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error creating settings: " + resultStatus.Description);
            if (settings == null)
                throw new Exception("Error creating settings: settings is null");
            settings.OutputFolder = tempFolder;
            settings.NcFileName = "example.nc";
            var resultGCodeFile = Path.Combine(settings.OutputFolder, settings.NcFileName);
            WriteLog("Resulting G code file: " + resultGCodeFile);
            
            // get postprocessor from all users documents folder
            var postProcessor = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                @"Ency\Version 1\Postprocessors\Mill\Sinumerik (840D)_Mill.sppx");
            if (!File.Exists(postProcessor))
                throw new Exception("Postprocessor not found: " + postProcessor);
            WriteLog("Postprocessor found: " + postProcessor);
            
            // generate CNC
            ncMaker.Generate(clDataFile, postProcessor, settings, out resultStatus);
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
        finally 
        {
            if (_logFile != null)
                Process.Start("notepad.exe", _logFile);
            
            // free COM objects
            if (settings != null)
                Marshal.ReleaseComObject(settings);
            if (ncMaker != null)
                Marshal.ReleaseComObject(ncMaker);
            if (technologist != null)
                Marshal.ReleaseComObject(technologist);
            if (operation != null)
                Marshal.ReleaseComObject(operation);
            if (project != null)
                Marshal.ReleaseComObject(project);
        }
    }
}