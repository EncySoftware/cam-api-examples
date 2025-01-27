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
    private static void AddCurves(ISTGeomFiler geomFile)
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

        geomFile.StartCurve3d("front", vertexLeftBottomFront);
        geomFile.CutTo3d(vertexLeftTopFront);
        geomFile.CutTo3d(vertexRightTopFront);
        geomFile.CutTo3d(vertexRightBottomFront);
        geomFile.CutTo3d(vertexLeftBottomFront);
        geomFile.CloseCurve3d(true);
        geomFile.AddEntity("front", "curve3d(front)");
        
        geomFile.StartCurve3d("back", vertexLeftBottomBack);
        geomFile.CutTo3d(vertexLeftTopBack);
        geomFile.CutTo3d(vertexRightTopBack);
        geomFile.CutTo3d(vertexRightBottomBack);
        geomFile.CutTo3d(vertexLeftBottomBack);
        geomFile.CloseCurve3d(true);
        geomFile.AddEntity("back", "curve3d(back)");
        
        geomFile.StartCurve3d("left_bottom", vertexLeftBottomFront);
        geomFile.CutTo3d(vertexLeftBottomBack);
        geomFile.CloseCurve3d(false);
        geomFile.AddEntity("left_bottom", "curve3d(left_bottom)");
        
        geomFile.StartCurve3d("right_bottom", vertexRightBottomFront);
        geomFile.CutTo3d(vertexRightBottomBack);
        geomFile.CloseCurve3d(false);
        geomFile.AddEntity("right_bottom", "curve3d(right_bottom)");
        
        geomFile.StartCurve3d("right_top", vertexRightTopFront);
        geomFile.CutTo3d(vertexRightTopBack);
        geomFile.CloseCurve3d(false);
        geomFile.AddEntity("right_top", "curve3d(right_top)");
        
        geomFile.StartCurve3d("left_top", vertexLeftTopFront);
        geomFile.CutTo3d(vertexLeftTopBack);
        geomFile.CloseCurve3d(false);
        geomFile.AddEntity("left_top", "curve3d(left_top)");
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
            
            // beginning of the file
            if (!geomFile.StartFile(sgfFilePath))
                throw new Exception("Can't start file: " + sgfFilePath);
            try
            {
                try
                {
                    // set point, we are going to use it as a coordinate system
                    geomFile.SetCurrentTransform(T3DMatrix.Unit.vT, T3DMatrix.Unit.vZ, T3DMatrix.Unit.vX);
                    
                    // item in geometry objects tree
                    geomFile.StartGroupEntity("curves");
                    try
                    {
                        AddCurves(geomFile);
                    }
                    finally
                    {
                        geomFile.CloseGroupEntity();
                    }
                }
                finally
                {
                    geomFile.CloseModel();
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
