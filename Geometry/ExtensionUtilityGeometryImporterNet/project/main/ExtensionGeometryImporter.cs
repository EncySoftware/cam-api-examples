using System.Runtime.InteropServices;
using CAMAPI.Application;
using CAMAPI.Extensions;
using CAMAPI.GeomImporter;
using CAMAPI.Project;
using CAMAPI.ResultStatus;

namespace ExtensionUtilityGeometryImporterNet;

/// <summary>
/// Utility to import geometry from Milling_25D\Part1.igs into the active project
/// </summary>
public class ExtensionGeometryImporter: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        ICamApiProject? activeProject = null;
        ICAMAPIGeometryImporter? importer = null;
        try
        {
            // active project
            activeProject = context.CamApplication.GetActiveProject(out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Can't get active project: " + resultStatus.Description);
            if (activeProject == null)
                throw new Exception("Active project is not found");
            
            // geometry importer
            importer = activeProject.GeomImporter;
            var modelFileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments),
                    @"ENCY\Version 1\Models\Milling_25D\Part1.igs");
            resultStatus = importer.ImportFile(modelFileName, @"", true);
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Can't import file: " + resultStatus.Description);
        } catch (Exception e) {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        } finally {
            // mandatory release of COM objects
            if (importer != null)
                Marshal.ReleaseComObject(importer);
            if (activeProject != null)
                Marshal.ReleaseComObject(activeProject);
        }
    }
}
