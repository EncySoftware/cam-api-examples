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
                // Import geometry model from standard installation folder 
                var importFileName = Path.Combine(paths.ModelsFolder, "Milling_3D", "49-1.igs");
                using var fullModel = new ComWrapper<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
                using var importer = new ComWrapper<ICAMAPIGeometryImporter>(activeProject.GeomImporter);

                importer.Instance.ImportFile(importFileName, "", false);
                var importedNodeName = fullModel.Instance.ActiveNode.FullName;

                // Example of nodes colorization by specified name and geometry type
                var desiredColor = 13132900;
                string[] desiredNames = ["2", "10", "28", "31"];
                using var nodeIterator = new ComWrapper<ICAMAPIGeometryTreeNodeIterator>(fullModel.Instance.GetNodes(out resultStatus));
                while (nodeIterator.Instance.MoveToChild()) {}
                do 
                {
                    var node = new ComWrapper<ICAMAPIGeometryTreeNode>(nodeIterator.Instance.Current());
                    if (node.Instance.GeometryEntity.EntityType == TCAMAPIGeometryEntityType.etFace)
                    {
                        var nodeName = node.Instance.Name;
                        foreach (var desiredName in desiredNames)
                            if (nodeName.EndsWith(desiredName))
                            {
                                node.Instance.GeometryEntity.Color = desiredColor;
                                // node.Instance.Selected = true;
                                break;
                            }
                    }
                } while(nodeIterator.Instance.MoveToSibling());

                // Example of nodes selection by specified color
                nodeIterator.Instance.Reset();
                while (nodeIterator.Instance.MoveToChild()) {}
                do 
                {
                    if (nodeIterator.Instance.Current().GeometryEntity.Color == desiredColor)
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
