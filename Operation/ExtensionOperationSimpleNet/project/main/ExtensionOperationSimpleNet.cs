using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using STCustomPropTypes;
using STTypes;
using STXMLPropTypes;

namespace ExtensionNetProject;

/// <summary>
/// Simple operation to make work path as rectangle or circle
/// </summary>
public class ExtensionOperationSimpleNet :
    IExtension,
    ICamApiTechOperationSolver
{
    /// <summary>
    /// Additional information about extension, provided in json file. It initializes in main CAM application
    /// </summary>
    public IExtensionInfo? Info { get; set; }
    
    /// <summary>
    /// Nothing to do
    /// </summary>
    public void InitSolver(ICamApiTechOperationSolverInitializeContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
    }
    
    /// <summary>
    /// Nothing to do
    /// </summary>
    public void FinalizeSolver()
    {
        
    }

    /// <inheritdoc />
    public bool GetPropIterator(string pageId, out IST_CustomPropIterator? iterator, out TResultStatus resultStatus)
    {
        resultStatus = default;
        iterator = null;
        return false;
    }

    /// <inheritdoc />
    public void OnPropFilterChanged(string parameterName, string value)
    {
        //
    }

    /// <summary>
    /// Delegate - how to make one layer of work path
    /// </summary>
    /// <param name="valueZ">Z value of layer</param>
    /// <returns>Last point to cut to</returns>
    private delegate TST3DPoint MakeOneLayer(double valueZ);

    /// <summary>
    /// ake simple work path according to operation params
    /// </summary>
    public void MakeWorkPath(ICamApiCLDReceiver cldFormer,
        ICamApiTechOperation techOperation,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            // read params
            using var xmlPropCom = new ComWrapper<IST_XMLPropPointer>(techOperation.XMLProp);
            var xmlProp = xmlPropCom.Instance
                ?? throw new Exception("Can't get XML properties");
            var pattern = xmlProp.Int["ToolpathParams.Pattern"];
            var layersCount = xmlProp.Int["ToolpathParams.ZLayers.Count"];
            var startZ = xmlProp.Flt["ToolpathParams.ZLayers.ZStart"];
            var stepZ = xmlProp.Flt["ToolpathParams.ZLayers.ZStep"];
            
            // get method to create one layer
            var makeOneLayer = pattern switch
            {
                // make work path as rectangle
                0 => MakeWorkPathRectangle(cldFormer, xmlProp),
                // make work path as circle
                1 => MakeWorkPathCircle(cldFormer, xmlProp),
                // unknown
                _ => null
            };
            if (makeOneLayer == null)
                return;
            
            // make layers
            var lastPoint = default(TST3DPoint);
            for (var i = 0; i < layersCount; i++)
                lastPoint = makeOneLayer(startZ + i * stepZ);
            
            // go to start point
            cldFormer.OutStandardFeed((int)TFeedTypeFlag.affRapid);
            lastPoint.Z = startZ + stepZ;
            cldFormer.CutTo(lastPoint);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    private MakeOneLayer MakeWorkPathRectangle(ICamApiCLDReceiver cldFormer, IST_XMLPropPointer xmlProp)
    {
        TST2DPoint startPoint;
        var isFirstMove = true;
        startPoint.X = xmlProp.Flt["ToolpathParams.RectParams.StartPoint.X"];
        startPoint.Y = xmlProp.Flt["ToolpathParams.RectParams.StartPoint.Y"];
        var width = xmlProp.Flt["ToolpathParams.RectParams.Width"];
        var height = xmlProp.Flt["ToolpathParams.RectParams.Height"];
        
        return
            curZ => {
                if (isFirstMove)
                {
                    isFirstMove = false;
                    cldFormer.OutStandardFeed((int)TFeedTypeFlag.affRapid);
                }
                else
                    cldFormer.OutStandardFeed((int)TFeedTypeFlag.affPlunge);
                
                var p = new TST3DPoint
                {
                    X = startPoint.X, Y = startPoint.Y, Z = curZ
                };
                cldFormer.CutTo(p);
                cldFormer.OutStandardFeed((int)TFeedTypeFlag.affWorking);
                p.X += width;
                cldFormer.CutTo(p);
                p.Y += height;
                cldFormer.CutTo(p);
                p.X -= width;
                cldFormer.CutTo(p);
                p.Y -= height;
                cldFormer.CutTo(p);
                
                return p;
            };
    }

    private MakeOneLayer MakeWorkPathCircle(ICamApiCLDReceiver cldFormer, IST_XMLPropPointer xmlProp)
    {
        TST2DPoint centerPoint;
        var isFirstMove = true;
        centerPoint.X = xmlProp.Flt["ToolpathParams.CircParams.CenterPoint.X"];
        centerPoint.Y = xmlProp.Flt["ToolpathParams.CircParams.CenterPoint.Y"];
        var radius = 0.5 * xmlProp.Flt["ToolpathParams.CircParams.Diameter"];
        
        return
            curZ => {
                if (isFirstMove) {
                    isFirstMove = false;
                    cldFormer.OutStandardFeed((int)TFeedTypeFlag.affRapid);
                } else
                    cldFormer.OutStandardFeed((int)TFeedTypeFlag.affPlunge);
                var pc = new TST3DPoint { X = centerPoint.X, Y = centerPoint.Y, Z = curZ };
                var p1 = new TST3DPoint { X = pc.X-radius, Y = pc.Y, Z = curZ };
                var p2 = new TST3DPoint { X = pc.X+radius, Y = pc.Y, Z = curZ };
                cldFormer.CutTo(p1);
                cldFormer.OutStandardFeed((int)TFeedTypeFlag.affWorking);
                cldFormer.ArcTo2d(p2, pc, TCLDPlaneType.aplXY, radius, false);
                cldFormer.ArcTo2d(p1, pc, TCLDPlaneType.aplXY, radius, false);
                return p1;
            };
    }
}