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
/// Utility to import vertexes and coordinate system
/// </summary>
public class ExtensionImportVertex: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }
    
    private static void CreatePoint(ISTGeomFiler geomFile, string pointName, TST3DPoint point)
    {
        geomFile.CreatePoint(pointName, point);
        geomFile.AddEntity(pointName, $"point({point.X}; {point.Y}; {point.Z})");
    }

    private static void CreateCoordinateSystem(ISTGeomFiler geomFile,
        string name,
        TST3DPoint vertex1,
        TST3DPoint vertex2,
        TST3DPoint vertex3,
        TST3DPoint vertex4)
    {
        var center = new TST3DPoint
        {
            X = (vertex1.X + vertex2.X + vertex3.X + vertex4.X) / 4,
            Y = (vertex1.Y + vertex2.Y + vertex3.Y + vertex4.Y) / 4,
            Z = (vertex1.Z + vertex2.Z + vertex3.Z + vertex4.Z) / 4
        };
        
        var vZ = new TST3DPoint
        {
            X = 0,
            Y = 0,
            Z = 1
        };
        
        var vX = new TST3DPoint
        {
            X = 1,
            Y = 0,
            Z = 0
        };

        geomFile.CreateCoordinateSystem(name, center, vZ, vX);
        geomFile.AddEntity(name, $"coordinateSystem({name})");
    }

    /// <summary>
    /// Add:
    /// 1. Vertexes of brick
    /// 2. Coordinate systems on center of each face
    /// </summary>
    private static void AddVertexesCoordinateSystems(ISTGeomFiler geomFile)
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
        
        // add vertexes
        CreatePoint(geomFile, "vertexLeftBottomFront", vertexLeftBottomFront);
        CreatePoint(geomFile, "vertexRightBottomFront", vertexRightBottomFront);
        CreatePoint(geomFile, "vertexRightTopFront", vertexRightTopFront);
        CreatePoint(geomFile, "vertexLeftTopFront", vertexLeftTopFront);
        CreatePoint(geomFile, "vertexLeftBottomBack", vertexLeftBottomBack);
        CreatePoint(geomFile, "vertexRightBottomBack", vertexRightBottomBack);
        CreatePoint(geomFile, "vertexRightTopBack", vertexRightTopBack);
        CreatePoint(geomFile, "vertexLeftTopBack", vertexLeftTopBack);
        
        // add coordinate systems
        CreateCoordinateSystem(geomFile, "coordinateSystemLeft",
            vertexLeftTopBack,
            vertexLeftTopFront,
            vertexLeftBottomBack,
            vertexLeftBottomFront);
        CreateCoordinateSystem(geomFile, "coordinateSystemRight",
            vertexRightTopBack,
            vertexRightTopFront,
            vertexRightBottomBack,
            vertexRightBottomFront);
        CreateCoordinateSystem(geomFile, "coordinateSystemTop",
            vertexRightTopBack,
            vertexRightTopFront,
            vertexLeftTopBack,
            vertexLeftTopFront);
        CreateCoordinateSystem(geomFile, "coordinateSystemBottom",
            vertexRightBottomBack,
            vertexRightBottomFront,
            vertexLeftBottomBack,
            vertexLeftBottomFront);
        CreateCoordinateSystem(geomFile, "coordinateSystemFront",
            vertexRightTopFront,
            vertexRightBottomFront,
            vertexLeftTopFront,
            vertexLeftBottomFront);
        CreateCoordinateSystem(geomFile, "coordinateSystemBack",
            vertexRightTopBack,
            vertexRightBottomBack,
            vertexLeftTopBack,
            vertexLeftBottomBack);
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var sgfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "example_vertexes.sgf");
            
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
                    geomFile.StartGroupEntity("vertexes");
                    try
                    {
                        AddVertexesCoordinateSystems(geomFile);
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
