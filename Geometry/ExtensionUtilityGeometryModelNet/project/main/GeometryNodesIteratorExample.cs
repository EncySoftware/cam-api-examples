using CAMAPI.Extensions;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.ResultStatus;
using System.Runtime.InteropServices;

namespace ExtensionUtilityGeometryModelNet;

public class GeometryNodesIteratorExample : IExtension, IExtensionUtility
{
    public IExtensionInfo? Info { get; set; }
    private const int nodeColor = 13132900;
    private readonly string[] nodeNames = { "Part\\49-1.igs\\Face2", "Part\\49-1.igs\\Face10", "Part\\49-1.igs\\Face28", "Part\\49-1.igs\\Face31" };

    public void Run(IExtensionUtilityContext Context, out TResultStatus result)
    {
        result = default;      
        var prj = Context.CamApplication.GetActiveProject(out TResultStatus r);
        try
        {
            if (r.Code == TResultStatusCode.rsSuccess)
            {
                using var fullModel = new ApiComObject<ICAMAPIGeometryModel>(prj.CAMAPIGeomModel);

                foreach (var nodeName in nodeNames)
                {                    
                    var geomNode = fullModel.Instance.FindByFullName(nodeName, out result);
                    geomNode.GeometryEntity.Color = nodeColor;
                    Marshal.ReleaseComObject(geomNode);
                }

                using var nodeIterator = new ApiComObject<ICAMAPIGeometryTreeNodeIterator>(fullModel.Instance.GetNodes(out result));
                while (nodeIterator.Instance.MoveToChild()) {}

                do 
                {
                    if (nodeIterator.Instance.Current().GeometryEntity.Color == nodeColor)
                        nodeIterator.Instance.Current().Selected = true;
                } while(nodeIterator.Instance.MoveToSibling());

                if (!(result.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(result.Description);
            }
        } finally {
            Marshal.ReleaseComObject(prj);
        }
    }
}
