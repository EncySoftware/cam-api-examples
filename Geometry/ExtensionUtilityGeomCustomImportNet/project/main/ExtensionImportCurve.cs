using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;
using Geometry.VecMatrLib;
using STGeomApiTypes;
using STTypes;

namespace ExtensionUtilityGeomCustomImportNet;

/// <summary>
/// Utility to import curves
/// </summary>
public class ExtensionImportCurve: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    /// <summary>
    /// Add curves to build:
    /// 1. 2 rectangles (front and back)
    /// 2. 4 lines (left_bottom, right_bottom, right_top, left_top)
    /// </summary>
    private static void AddCurves(ISTGeomReceiver geomReceiver)
    {
        const int length = 1;
        const int width = 2;
        const int height = 3;

        var vertexLeftBottomFront = new TST3DPoint { X = 0, Y = 0, Z = 0 };
        var vertexRightBottomFront = new TST3DPoint { X = length, Y = 0, Z = 0 };
        var vertexRightTopFront = new TST3DPoint { X = length, Y = width, Z = 0 };
        var vertexLeftTopFront = new TST3DPoint { X = 0, Y = width, Z = 0 };
        var vertexLeftBottomBack = new TST3DPoint { X = 0, Y = 0, Z = height };
        var vertexRightBottomBack = new TST3DPoint { X = length, Y = 0, Z = height };
        var vertexRightTopBack = new TST3DPoint { X = length, Y = width, Z = height };
        var vertexLeftTopBack = new TST3DPoint { X = 0, Y = width, Z = height };

        geomReceiver.StartCurve3d("front", vertexLeftBottomFront);
        geomReceiver.CutTo3d(vertexLeftTopFront);
        geomReceiver.CutTo3d(vertexRightTopFront);
        geomReceiver.CutTo3d(vertexRightBottomFront);
        geomReceiver.CutTo3d(vertexLeftBottomFront);
        geomReceiver.CloseCurve3d(true);
        geomReceiver.AddEntity("front", "curve3d(front)");
        
        geomReceiver.StartCurve3d("back", vertexLeftBottomBack);
        geomReceiver.CutTo3d(vertexLeftTopBack);
        geomReceiver.CutTo3d(vertexRightTopBack);
        geomReceiver.CutTo3d(vertexRightBottomBack);
        geomReceiver.CutTo3d(vertexLeftBottomBack);
        geomReceiver.CloseCurve3d(true);
        geomReceiver.AddEntity("back", "curve3d(back)");
        
        geomReceiver.StartCurve3d("left_bottom", vertexLeftBottomFront);
        geomReceiver.CutTo3d(vertexLeftBottomBack);
        geomReceiver.CloseCurve3d(false);
        geomReceiver.AddEntity("left_bottom", "curve3d(left_bottom)");
        
        geomReceiver.StartCurve3d("right_bottom", vertexRightBottomFront);
        geomReceiver.CutTo3d(vertexRightBottomBack);
        geomReceiver.CloseCurve3d(false);
        geomReceiver.AddEntity("right_bottom", "curve3d(right_bottom)");
        
        geomReceiver.StartCurve3d("right_top", vertexRightTopFront);
        geomReceiver.CutTo3d(vertexRightTopBack);
        geomReceiver.CloseCurve3d(false);
        geomReceiver.AddEntity("right_top", "curve3d(right_top)");
        
        geomReceiver.StartCurve3d("left_top", vertexLeftTopFront);
        geomReceiver.CutTo3d(vertexLeftTopBack);
        geomReceiver.CloseCurve3d(false);
        geomReceiver.AddEntity("left_top", "curve3d(left_top)");
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var sgfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "example_curves.sgf");
            
            // create object to write geometry
            using var wrapperFactoryGeomFiler = SystemExtensionFactory.GetSingletonExtension<ICamApiFactoryGeometryFile>("Extension.Global.Singletons.GeomFile");
            var factoryGeomFiler = wrapperFactoryGeomFiler.Instance
                                   ?? throw new Exception("Can't get geometry filer singleton");
            using var wrapperGeomFile = new ComWrapper<ISTGeomFiler>(factoryGeomFiler.CreateObject());
            var geomFile = wrapperGeomFile.Instance
                           ?? throw new Exception("Can't create geometry filer object");
            
            if (geomFile is not ISTGeomReceiver geomReceiver)
                throw new Exception("Can`t cast geomFile to geomReceiver");
               
            // beginning of the file
            if (!geomFile.StartFile(sgfFilePath))
                throw new Exception("Can't start file: " + sgfFilePath);
            try
            {
                try
                {
                    // set point, we are going to use it as a coordinate system
                    geomReceiver.SetCurrentTransform(T3DMatrix.Unit.vT, T3DMatrix.Unit.vZ, T3DMatrix.Unit.vX);
                    
                    // item in geometry objects tree
                    geomReceiver.StartGroupEntity("curves");
                    try
                    {
                        AddCurves(geomReceiver);
                    }
                    finally
                    {
                        geomReceiver.CloseGroupEntity();
                    }
                }
                finally
                {
                    geomReceiver.CloseModel();
                }
            }
            finally
            {
                geomFile.CloseFile();
            }
            
            ImportHelper.Import(context, sgfFilePath);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
