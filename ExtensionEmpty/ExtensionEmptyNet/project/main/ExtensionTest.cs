﻿using System.Diagnostics;
using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionNetProject;

/// <summary>
/// Show parameters of project
/// </summary>
public class ExtensionTest : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Show parameters of project
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // get project
            var project = context.CamApplication.GetActiveProject(out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error getting project: " + resultStatus.Description);

            // save params in some temp file to show it later
            var tempFile = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".txt");
            File.WriteAllText(tempFile, "Project file path: " + project.FilePath + Environment.NewLine);
            File.AppendAllText(tempFile, "Project id: " + project.Id);
            
            // show temp file
            Process.Start("notepad.exe", tempFile);
            
            // free memory
            Marshal.ReleaseComObject(project);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}