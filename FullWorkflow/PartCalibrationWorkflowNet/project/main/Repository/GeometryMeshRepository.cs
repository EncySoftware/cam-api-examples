using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.Project;
using PartCalibrationWorkflowNet.Model;
using STTypes;

namespace PartCalibrationWorkflowNet.Repository;

/// <summary>
/// Reads geometry mesh data from the project and builds a point cloud from triangle vertices.
/// Ported from ProbeSetupApp.Repository.GeometryMeshRepository.
/// </summary>
internal static class GeometryMeshRepository
{
    private const double TessTol           = 0.1;
    private const double WorkpieceTxOffset = 20.0;

    /// <summary>
    /// Collects all triangle vertices from the project geometry model.
    /// Applies the Ry=-90° world transform (same as PrepareProjectService.WorkpieceOffset).
    /// </summary>
    public static List<SurfacePoint> BuildCloud(ComWrapper<ICamApiProject> projCom)
    {
        var cloud = new List<SurfacePoint>();
        using var geomCom = projCom.CAMAPIGeomModel();

        // EnumerateNodes uses AsComEnumerable — do NOT add 'using (nodeCom)' in body
        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != TCAMAPIGeometryEntityType.etFace) continue;

            var fullName = nodeCom.FullName();
            entityCom.Invoke(entity =>
            {
                if (entity is not ICamApiFaceGeometryEntity faceEntity) return;

                using var faceCom = ComWrapper.Create(faceEntity.Face);
                using var meshCom = faceCom.GetMesh(TessTol);

                int triCount = meshCom.GetTriangleCount();
                for (int i = 0; i < triCount; i++)
                {
                    var tri    = meshCom.GetTriangle(i);
                    var normal = meshCom.GetTriangleNormal(i);
                    foreach (var v in new[] { meshCom.GetVertex(tri.X), meshCom.GetVertex(tri.Y), meshCom.GetVertex(tri.Z) })
                    {
                        var (wp, wn) = ToWorld(v, normal);
                        cloud.Add(new SurfacePoint(v, normal, wp, wn, fullName));
                    }
                }
            });
        }
        return cloud;
    }

    /// <summary>World transform: Ry=-90°, tx=WorkpieceTxOffset.</summary>
    private static (TST3DPoint WorldPos, TST3DPoint WorldNormal) ToWorld(
        TST3DPoint pos, TST3DPoint normal) =>
    (
        new TST3DPoint { X = -pos.Z + WorkpieceTxOffset, Y = pos.Y, Z = pos.X },
        new TST3DPoint { X = -normal.Z,                  Y = normal.Y, Z = normal.X }
    );
}
