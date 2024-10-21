using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.GeomModel;
using CAMAPI.GeomImporter;
using CAMAPI.ResultStatus;
using CAMAPI.Project;
using CAMAPI.Singletons;
using System.Runtime.InteropServices;

namespace ExtensionUtilityGeometryModelNet;

/// <summary>
/// Utility to change color of geometry nodes and select them
/// </summary>
public class GeometryNodesIteratorExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext Context, out TResultStatus resultStatus)
    {        
        resultStatus = default;
        ICamApiProject? activeProject = null;

        try
        {
            // get global context
            var extension = Info?.InstanceInfo.ExtensionManager.GetSingletonExtension("Extension.Global.Singletons.Paths", out resultStatus)
                            ?? throw new Exception("Info is null");
            if (resultStatus.Code == TResultStatusCode.rsError)
                throw new Exception("Error getting global context: " + resultStatus.Description);
            if (extension is not ICamApiPaths paths)
                throw new Exception("Cannot get global context");

            activeProject = Context.CamApplication.GetActiveProject(out resultStatus);

            if (resultStatus.Code == TResultStatusCode.rsSuccess)
            {
                var importFileName = Path.Combine(paths.ModelsFolder, "Milling_3D", "49-1.igs");
                var nodeColor = 13132900;
                string[] nodeNames = ["Part\\49-1.igs\\Face2", "Part\\49-1.igs\\Face10", "Part\\49-1.igs\\Face28", "Part\\49-1.igs\\Face31"];
                using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
                using var importer = new ApiComObject<ICAMAPIGeometryImporter>(activeProject.GeomImporter);

                importer.Instance.ImportFile(importFileName, "", false);

                foreach (var nodeName in nodeNames)
                {                    
                    var geomNode = fullModel.Instance.FindByFullName(nodeName, out resultStatus);
                    geomNode.GeometryEntity.Color = nodeColor;
                    Marshal.ReleaseComObject(geomNode);
                }

                using var nodeIterator = new ApiComObject<ICAMAPIGeometryTreeNodeIterator>(fullModel.Instance.GetNodes(out resultStatus));
                while (nodeIterator.Instance.MoveToChild()) {}

                do 
                {
                    if (nodeIterator.Instance.Current().GeometryEntity.Color == nodeColor)
                        nodeIterator.Instance.Current().Selected = true;
                } while(nodeIterator.Instance.MoveToSibling());

                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);
            }
        } finally {
            if (activeProject != null)
                Marshal.ReleaseComObject(activeProject);
        }
    }
}
