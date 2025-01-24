using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.GeomModel;
using CAMAPI.GeomImporter;
using CAMAPI.ResultStatus;
using CAMAPI.Project;
using CAMAPI.Singletons;
using STTypes;
using System.Runtime.InteropServices;
using Geometry.VecMatrLib;

namespace ExtensionUtilityGeometryModelNet;

/// <summary>
/// Utility to transform geometry model using transformation matrices
/// </summary>
public class GeometryNodeTransformExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    private static TST3DMatrix MakeScaleMatrix(double scaleValue, double shiftX, double shiftY, double shiftZ)
    {
        TST3DMatrix matrix = T3DMatrix.Unit;
        matrix.vX.X *= scaleValue;
        matrix.vY.Y *= scaleValue;
        matrix.vZ.Z *= scaleValue;
        matrix.vT.X = shiftX;  
        matrix.vT.Y = shiftY;
        matrix.vT.Z = shiftZ;
        return matrix;
    }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext Context, out TResultStatus resultStatus)
    {        
        resultStatus = default;
        ICamApiProject? activeProject = null;

        try
        {
            // get global context
            var pathsCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
            var paths = pathsCom.Instance
                ?? throw new Exception("Cannot get global context");

            activeProject = Context.CamApplication.GetActiveProject(out resultStatus);
            if (resultStatus.Code == TResultStatusCode.rsSuccess)
            {
                var importFileName = Path.Combine(paths.ModelsFolder, "Milling_3D", "49-1.igs");
                using var fullModel = new ComWrapper<ICAMAPIGeometryModel>(activeProject.CAMAPIGeomModel);
                using var importer = new ComWrapper<ICAMAPIGeometryImporter>(activeProject.GeomImporter);

                importer.Instance.ImportFile(importFileName, "", false);
                var nodeFullName = fullModel.Instance.ActiveNode.FullName;
                var geomNode = new ComWrapper<ICAMAPIGeometryTreeNode>(fullModel.Instance.FindByFullName(nodeFullName, out resultStatus));

                var shiftMatrix = T3DMatrix.MakeShiftMatrix(new T3DPoint { X = 100, Y = 200, Z = 0 });
                resultStatus = fullModel.Instance.Transform(geomNode.Instance, shiftMatrix);
                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);

                var rotMatrix = T3DMatrix.MakeRotMatrix(60, 1, T3DPoint.Zero);
                resultStatus = fullModel.Instance.Transform(geomNode.Instance, rotMatrix);
                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);

                var scaleMatrix = MakeScaleMatrix(2, 100, 200, 0);
                resultStatus = fullModel.Instance.Transform(geomNode.Instance, scaleMatrix);
                if (!(resultStatus.Code == TResultStatusCode.rsSuccess))
                    throw new Exception(resultStatus.Description);
            }
        } finally {
            if (activeProject != null)
                Marshal.ReleaseComObject(activeProject);
        }
    }
}
