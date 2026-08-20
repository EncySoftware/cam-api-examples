using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.Project;
using CAMAPI.TechSolvers;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Projects measured 3D points onto the nearest positions on the project's CAD geometry
/// using <see cref="ICamApiPointSnapper"/>. Returns snapped (nominal) points that correspond
/// 1:1 by index to the input measured points.
/// </summary>
internal sealed class SurfaceProjectionService : IDisposable
{
    private const double SnapTolerance = 0.1;

    private readonly ComWrapper<ICamApiPointSnapper> _snapperCom;

    public SurfaceProjectionService()
    {
        _snapperCom = PointSnapperHelper.GetSingleton();
    }

    public void Dispose() => _snapperCom.Dispose();

    /// <summary>
    /// For each point in <paramref name="measuredPoints"/>, finds the nearest position
    /// on any face of the project's geometry model.
    /// </summary>
    public TST3DPoint[] SnapToModel(
        ComWrapper<ICamApiProject> projCom,
        TST3DPoint[] measuredPoints)
    {
        using var builderCom = _snapperCom.CreateFaceList();
        CollectFaces(projCom, builderCom);
        using var facesCom = builderCom.Build();

        using var pointsCom = _snapperCom.CreatePointList();
        foreach (var pt in measuredPoints)
            pointsCom.Add(pt);

        return _snapperCom.FindNearestOnFaces(facesCom, pointsCom, SnapTolerance);
    }

    private static void CollectFaces(
        ComWrapper<ICamApiProject> projCom,
        ComWrapper<ICamApiFaceListBuilder> builderCom)
    {
        using var geomCom = projCom.CAMAPIGeomModel();

        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != TCAMAPIGeometryEntityType.etFace) continue;

            entityCom.Invoke(entity =>
            {
                if (entity is not ICamApiFaceGeometryEntity faceEntity) return;
                using var faceCom = ComWrapper.Create(faceEntity.Face);
                builderCom.Add(faceCom);
            });
        }
    }
}
