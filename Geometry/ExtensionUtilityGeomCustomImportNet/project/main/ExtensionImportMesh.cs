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
public class ExtensionImportMesh: IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Add rectangle as 6 meshes
    /// </summary>
    private static void AddMeshes(ISTGeomFiler geomFile)
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
        
        geomFile.StartMesh("brick");

        geomFile.AddMeshVertex(0, vertexLeftBottomFront);
        geomFile.AddMeshVertex(1, vertexRightBottomFront);
        geomFile.AddMeshVertex(2, vertexRightTopFront);
        geomFile.AddMeshVertex(3, vertexLeftTopFront);
        geomFile.AddMeshVertex(4, vertexLeftBottomBack);
        geomFile.AddMeshVertex(5, vertexRightBottomBack);
        geomFile.AddMeshVertex(6, vertexRightTopBack);
        geomFile.AddMeshVertex(7, vertexLeftTopBack);

        geomFile.AddMeshTriangle(0, 2, 1);
        geomFile.AddMeshTriangle(0, 3, 2);

        geomFile.AddMeshTriangle(4, 6, 5);
        geomFile.AddMeshTriangle(4, 7, 6);

        geomFile.AddMeshTriangle(0, 5, 1);
        geomFile.AddMeshTriangle(0, 4, 5);

        geomFile.AddMeshTriangle(1, 6, 2);
        geomFile.AddMeshTriangle(1, 5, 6);

        geomFile.AddMeshTriangle(2, 7, 3);
        geomFile.AddMeshTriangle(2, 6, 7);

        geomFile.AddMeshTriangle(3, 4, 0);
        geomFile.AddMeshTriangle(3, 7, 4);

        geomFile.CloseMesh();
        geomFile.AddEntity("brick", "brick");
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            var sgfFilePath = Path.Combine(Directory.GetCurrentDirectory(), "example_meshes.sgf");

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
            
            geomFile.StartModel();
            try
            {
                try
                {
                    // set point, we are going to use it as a coordinate system
                    geomFile.SetCurrentTransform(T3DMatrix.Unit.vT, T3DMatrix.Unit.vZ, T3DMatrix.Unit.vX);
                    
                    // item in geometry objects tree
                    geomFile.StartGroupEntity("meshes");
                    try
                    {
                        AddMeshes(geomFile);
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
