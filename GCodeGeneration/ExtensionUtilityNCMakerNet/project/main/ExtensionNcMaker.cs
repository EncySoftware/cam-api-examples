﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.NCMaker;
using CAMAPI.Project;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using CAMAPI.Technologist;
using CAMAPI.DotnetHelper;

namespace ExtensionUtilityNcMakerNet;

/// <summary>
/// Extension for exampling - how to generate G code
/// </summary>
public class ExtensionNcMaker : IExtension, IExtensionUtility
{
    private string _logFileName = "";
    private string _tempDir = "";

    /// <summary>
    /// Additional information about extension, provided in json file. It initializes in main CAM application
    /// </summary>
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        var resultGCodeFile = "";
        resultStatus = default;
        
        try
        {
            // global context
            using var pathsHelper = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths", Info);

            // Make temp file name to write log
            _tempDir = Path.Combine(Path.GetTempPath(), "MakeNCUtilityExtension", Path.GetRandomFileName());
            Directory.CreateDirectory(_tempDir);
            _logFileName = Path.Combine(_tempDir, "log.txt");
            WriteLog("MakeNCUtilityExtension log started");

            // catch active project
            using var projectCom = new ApiComObject<ICamApiProject>(context.CamApplication.GetActiveProject(out resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error getting active project: " + resultStatus.Description);
            var project = projectCom.Instance;
            
            using var technologistCom = new ApiComObject<ICamApiTechnologist>(project.Technologist);
            var technologist = technologistCom.Instance;
            using var ncMakerCom = new ApiComObject<ICamApiNCMaker>(project.NCMaker);
            var ncMaker = ncMakerCom.Instance;
            
            // no active project
            if (project == null)
                throw new Exception("No active project");
            
            // show cam info in console
            WriteLog("Active project file: " + project.FilePath);
            WriteLog("Active project ID: " + project.Id);
            
            using var operationCom = new ApiComObject<ICamApiTechOperationIterator>(technologist.GetOperations(TCamApiReorderingMode.rmReordered, out resultStatus));
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error getting operations: " + resultStatus.Description);
            var operations = operationCom.Instance;

            // Limit set of operations by substring inside full name
            operations.OperationsFilter = new OperationsFilterByName("Setup stage 1");

            // make CLData
            var clDataFile = Path.Combine(_tempDir, "example.inpcld");
            project.SaveClData(clDataFile, operations, out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error saving CLData: " + resultStatus.Description);
            WriteLog("CLData saved to file: " + clDataFile);

            // make settings for CNC generating
            var settings = ncMaker.CreateSettings(TCamApiNCMakerSettingsType.ncsSppx, out resultStatus) as ICamApiMakeCncSppxSettings;
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error creating settings: " + resultStatus.Description);
            if (settings == null)
                throw new Exception("Error creating settings: settings is null");
            settings.OutputFolder = _tempDir;
            settings.NcFileName = "example.nc";
            resultGCodeFile = Path.Combine(settings.OutputFolder, settings.NcFileName);
            WriteLog("Resulting G code file: " + Path.Combine(settings.OutputFolder, settings.NcFileName));
            
            // get postprocessor from all users documents folder
            var postProcessor = Path.Combine(pathsHelper.Instance.PostprocessorsFolder, "Mill", "Sinumerik (840D)_Mill.sppx");
            if (!File.Exists(postProcessor))
                throw new Exception("Postprocessor not found: " + postProcessor);
            WriteLog("Postprocessor found: " + postProcessor);
            
            // generate CNC
            ncMaker.Generate(clDataFile, postProcessor, settings, out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error generating CNC: " + resultStatus.Description);
            WriteLog("CNC successfully generated");
            WriteLog(resultStatus.Description);
        }
        catch (Exception e)
        {
            WriteLog("Error: " + Environment.NewLine + e.Message);
        }
        finally 
        {
            Process.Start("notepad.exe", _logFileName);
            if (File.Exists(resultGCodeFile))
                Process.Start("notepad.exe", resultGCodeFile);
        }
    }

    private void WriteLog(string line) {
        Console.WriteLine(line);
        File.AppendAllText(_logFileName, line + Environment.NewLine);
    }
}
