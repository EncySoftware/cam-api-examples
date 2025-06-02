using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.MCDFormerTypes;
using CAMAPI.ResultStatus;
using CAMAPI.TechOperation;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;
using STTypes;

namespace ExtensionNetProject;

/// <summary>
/// Operation to make work path as rectangle or circle. Params are read from GetPropIterator method
/// </summary>
public class ExtensionOperationParams :
    IExtension,
    ICamApiTechOperationSolver
{
    /// <summary>
    /// Additional information about extension, provided in json file. It initializes in main CAM application
    /// </summary>
    public IExtensionInfo? Info { get; set; }
    
    /// <summary>
    /// Wrapper over COM object, which is current operation
    /// </summary>
    private ComWrapper<ICamApiTechOperation>? _operationCom;
    
    /// <summary>
    /// Wrapper over iterator of operation properties. We need it to create dynamic properties
    /// </summary>
    private ComWrapper<IST_SimplePropIterator>? _propIteratorCom;
    
    private OperationCustomPropsHelper? _customPropsHelper;

    /// <summary>
    /// Subscribe on events from operation. Setting available items in job assignment
    /// </summary>
    public void InitSolver(ICamApiTechOperationSolverInitializeContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            _operationCom = ComWrapper.Create(context.TechOperation);
            _customPropsHelper = new OperationCustomPropsHelper();
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Clear memory and unsubscribe from events
    /// </summary>
    public void FinalizeSolver()
    {
        _customPropsHelper?.Dispose();
        _propIteratorCom?.Dispose();
        _operationCom?.Dispose();
    }

    /// <inheritdoc />
    public bool GetPropIterator(string pageId, out IST_CustomPropIterator? iterator, out TResultStatus resultStatus)
    {
        resultStatus = default;
        iterator = null;
        
        try
        {
            _operationCom?.Invoke(operation =>
            {
                if (_customPropsHelper == null)
                    throw new Exception("Custom properties helper is not initialized");

                // recreate iterator with mandatory disposing old one
                _propIteratorCom?.Dispose();
                using var propHelpersCom =
                    SystemExtensionFactory.GetSingletonExtension<IST_CustomPropHelpers>("Extension.CustomPropHelpers");
                _propIteratorCom = propHelpersCom.InvokeAndWrap(propHelpers => propHelpers.CreateSimplePropIterator());

                // params
                _customPropsHelper.AddEnumWithIdParam(_operationCom, _propIteratorCom,
                    "Toolpath pattern", "ToolpathParams.Pattern", new Dictionary<string, string>()
                    {
                        ["Rect"] = "Rectangle",
                        ["Circle"] = "Circle"
                    });
                var isRectangle = new BooleanValueGetter(() =>
                    _customPropsHelper.ReadEnumWithIdParam(operation, "ToolpathParams.Pattern") == "Rect");
                var isCircle = new BooleanValueGetter(() =>
                    _customPropsHelper.ReadEnumWithIdParam(operation, "ToolpathParams.Pattern") == "Circle");

                // rectangle parameters
                var rectParamsPropIndex = _customPropsHelper.AddComplexProp(_propIteratorCom,
                    "Rectangle parameters", "RectParams", isRectangle);
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "X", "ToolpathParams.RectParams.StartPoint.X", 10.0,
                    isRectangle,
                    rectParamsPropIndex);
                
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "Y", "ToolpathParams.RectParams.StartPoint.Y", 10.0,
                    isRectangle,
                    rectParamsPropIndex);
                
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "Width", "ToolpathParams.RectParams.Width", 90.0,
                    isRectangle,
                    rectParamsPropIndex);
                
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "Height", "ToolpathParams.RectParams.Height", 90.0,
                    isRectangle,
                    rectParamsPropIndex);
                    
                // circle parameters
                var circParamsPropIndex = _customPropsHelper.AddComplexProp(_propIteratorCom,
                    "Circle parameters", "CircParams", isCircle);
                
                var circCenterPointIndex = _customPropsHelper.AddComplexProp(_propIteratorCom,
                    "Center point", "CenterPoint", isCircle, circParamsPropIndex);
                
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "X", "ToolpathParams.CircParams.CenterPoint.X", 50.0,
                    isCircle,
                    circCenterPointIndex);
                
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "Y", "ToolpathParams.CircParams.CenterPoint.Y", 50.0,
                    isCircle,
                    circCenterPointIndex);
                
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "Diameter", "ToolpathParams.CircParams.Diameter", 45.0,
                    isCircle,
                    circCenterPointIndex);
                    
                // layers params
                var layersPropIndex = _customPropsHelper.AddComplexProp(_propIteratorCom,
                    "Z layers", "ZLayers");

                _customPropsHelper.AddIntegerProp(_operationCom, _propIteratorCom,
                    "Count", "ToolpathParams.ZLayers.Count", 10, null, layersPropIndex);
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "Z start", "ToolpathParams.ZLayers.ZStart", 100.0,
                    null, layersPropIndex);
                _customPropsHelper.AddDoubleProp(_operationCom, _propIteratorCom,
                    "Z step", "ToolpathParams.ZLayers.ZStep", -10.0,
                    null, layersPropIndex);
            });

            // build result
            _propIteratorCom?.Invoke(iterator => iterator.MoveToRoot());
            iterator = (IST_CustomPropIterator)_propIteratorCom?.Instance
                ?? throw new Exception("Can't get custom property iterator instance");
            return true;
        } 
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
            iterator = null;
            return false;
        }
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
    /// Make simple work path according to operation params
    /// </summary>
    public void MakeWorkPath(ICamApiCLDReceiver cldFormer,
        ICamApiTechOperation techOperation,
        out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            _operationCom?.Invoke(operation =>
            {
                if (_customPropsHelper == null)
                    throw new Exception("Custom properties helper is not initialized");
                
                // logging object
                using var loggerCom = ExtensionManagerHelper.GetInstance().InvokeAndWrap(manager => manager.Logger);
                var logger = loggerCom.Instance
                             ?? throw new Exception("Can't get logger instance");
                
                // read params
                using var xmlPropCom = ComWrapper.Create(techOperation.XMLProp);
                var pattern = _customPropsHelper.ReadEnumWithIdParam(operation, "ToolpathParams.Pattern");
                var layersCount = _customPropsHelper.ReadIntegerParam(operation, "ToolpathParams.ZLayers.Count");
                var startZ = _customPropsHelper.ReadDoubleParam(operation, "ToolpathParams.ZLayers.ZStart");
                var stepZ = _customPropsHelper.ReadDoubleParam(operation, "ToolpathParams.ZLayers.ZStep");
                cldFormer.AddComment("Go to start point");
            
                // get method to create one layer
                var makeOneLayer = pattern switch
                {
                    // make work path as rectangle
                    "Rect" => MakeWorkPathRectangle(cldFormer),
                    // make work path as circle
                    "Circle" => MakeWorkPathCircle(cldFormer),
                    // unknown
                    _ => null
                };
                logger.Info($"Make work path with pattern: {pattern} Operation: {techOperation.Name}");
            
                // make layers
                var lastPoint = default(TST3DPoint);
                for (var i = 0; i < layersCount; i++)
                    lastPoint = makeOneLayer(startZ + i * stepZ);
            
                // go to start point
                cldFormer.OutStandardFeed((int)TFeedTypeFlag.affRapid);
                lastPoint.Z = startZ + stepZ;
                cldFormer.CutTo(lastPoint);
            });
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    private MakeOneLayer MakeWorkPathRectangle(ICamApiCLDReceiver cldFormer)
    {
        if (_customPropsHelper == null)
            throw new Exception("Custom properties helper is not initialized");
        TST2DPoint startPoint;
        var isFirstMove = true;
        startPoint.X = _customPropsHelper.ReadDoubleParam(_operationCom?.Instance, "ToolpathParams.RectParams.StartPoint.X");
        startPoint.Y = _customPropsHelper.ReadDoubleParam(_operationCom?.Instance, "ToolpathParams.RectParams.StartPoint.Y");
        var width = _customPropsHelper.ReadDoubleParam(_operationCom?.Instance, "ToolpathParams.RectParams.Width");
        var height = _customPropsHelper.ReadDoubleParam(_operationCom?.Instance, "ToolpathParams.RectParams.Height");
        
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

    private MakeOneLayer MakeWorkPathCircle(ICamApiCLDReceiver cldFormer)
    {
        if (_customPropsHelper == null)
            throw new Exception("Custom properties helper is not initialized");
        TST2DPoint centerPoint;
        var isFirstMove = true;
        centerPoint.X = _customPropsHelper.ReadDoubleParam(_operationCom?.Instance, "ToolpathParams.CircParams.CenterPoint.X");
        centerPoint.Y = _customPropsHelper.ReadDoubleParam(_operationCom?.Instance, "ToolpathParams.CircParams.CenterPoint.Y");
        var radius = 0.5 * _customPropsHelper.ReadDoubleParam(_operationCom?.Instance, "ToolpathParams.CircParams.Diameter");
        
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