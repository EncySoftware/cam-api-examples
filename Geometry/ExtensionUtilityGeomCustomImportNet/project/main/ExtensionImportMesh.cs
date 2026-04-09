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
    private static void AddMeshes(ISTGeomReceiver geomReceiver)
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
        
        geomReceiver.StartMesh("brick");

        geomReceiver.AddMeshVertex(0, vertexLeftBottomFront);
        geomReceiver.AddMeshVertex(1, vertexRightBottomFront);
        geomReceiver.AddMeshVertex(2, vertexRightTopFront);
        geomReceiver.AddMeshVertex(3, vertexLeftTopFront);
        geomReceiver.AddMeshVertex(4, vertexLeftBottomBack);
        geomReceiver.AddMeshVertex(5, vertexRightBottomBack);
        geomReceiver.AddMeshVertex(6, vertexRightTopBack);
        geomReceiver.AddMeshVertex(7, vertexLeftTopBack);

        geomReceiver.AddMeshTriangle(0, 2, 1);
        geomReceiver.AddMeshTriangle(0, 3, 2);

        geomReceiver.AddMeshTriangle(4, 6, 5);
        geomReceiver.AddMeshTriangle(4, 7, 6);

        geomReceiver.AddMeshTriangle(0, 5, 1);
        geomReceiver.AddMeshTriangle(0, 4, 5);

        geomReceiver.AddMeshTriangle(1, 6, 2);
        geomReceiver.AddMeshTriangle(1, 5, 6);

        geomReceiver.AddMeshTriangle(2, 7, 3);
        geomReceiver.AddMeshTriangle(2, 6, 7);

        geomReceiver.AddMeshTriangle(3, 4, 0);
        geomReceiver.AddMeshTriangle(3, 7, 4);

        geomReceiver.CloseMesh();
        geomReceiver.AddEntity("brick", "brick");
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
            
            if (geomFile is not ISTGeomReceiver geomReceiver)
                throw new Exception("Can`t cast geomFile to geomReceiver");
                
            geomReceiver.StartModel();
            try
            {
                try
                {
                    // set point, we are going to use it as a coordinate system
                    geomReceiver.SetCurrentTransform(T3DMatrix.Unit.vT, T3DMatrix.Unit.vZ, T3DMatrix.Unit.vX);
                    
                    // item in geometry objects tree
                    geomReceiver.StartGroupEntity("meshes");
                    try
                    {
                        AddMeshes(geomReceiver);
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
