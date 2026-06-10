using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.Project;
using CAMAPI.SurfaceTypes;
using PartCalibrationWorkflowNet.Model;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Samples a point cloud over the faces currently selected on the viewport.
/// Points are distributed across selected faces proportional to triangle areas;
/// each point carries the surface normal at its triangle.
/// </summary>
internal static class PointSamplingService
{
    private const double TessTol = 0.1;

    /// <summary>
    /// Samples up to <paramref name="targetCount"/> points across all faces
    /// currently selected in the project geometry model.
    /// </summary>
    public static List<Point3D> Sample(
        ComWrapper<ICamApiProject> projCom,
        int targetCount)
    {
        if (targetCount <= 0)
            throw new ArgumentException("Number of points must be greater than zero.", nameof(targetCount));

        using var geomCom = projCom.CAMAPIGeomModel();
        using var facesCom = geomCom.GetFaceListOfSelected();
        var faceCount = facesCom.Invoke(list => list.Count);
        if (faceCount == 0)
            throw new InvalidOperationException(
                "No faces are selected on the viewport. Select one or more surfaces and try again.");

        // 1. Gather triangles from every selected face
        var triangles = CollectTriangles(facesCom, faceCount);
        if (triangles.Count == 0)
            throw new InvalidOperationException(
                "Selected faces have no tessellation triangles. Try increasing the tessellation tolerance.");

        // 2. Pick triangles by area-weighted sampling, place 1 point at each centroid
        double totalArea = triangles.Sum(t => t.Area);
        var result = new List<Point3D>(targetCount);
        double stride = totalArea / targetCount;
        double acc = stride / 2;
        int triIdx = 0;
        double consumed = 0;
        for (int i = 0; i < targetCount; i++)
        {
            while (triIdx < triangles.Count && consumed + triangles[triIdx].Area < acc)
            {
                consumed += triangles[triIdx].Area;
                triIdx++;
            }
            if (triIdx >= triangles.Count) triIdx = triangles.Count - 1;
            var tri = triangles[triIdx];
            result.Add(new Point3D
            {
                Name = $"Point{i + 1}",
                X = tri.Centroid.X, Y = tri.Centroid.Y, Z = tri.Centroid.Z,
                Nx = tri.Normal.X,  Ny = tri.Normal.Y,  Nz = tri.Normal.Z,
            });
            acc += stride;
        }
        return result;
    }

    private static List<TriangleSample> CollectTriangles(
        ComWrapper<ICamApiFaceList> facesCom,
        int faceCount)
    {
        var triangles = new List<TriangleSample>();
        for (int i = 0; i < faceCount; i++)
        {
            using var faceCom = facesCom.InvokeAndWrap(list => list.Face[i]);
            using var meshCom = faceCom.InvokeAndWrap(f => f.GetMesh(TessTol));
            meshCom.Invoke(mesh =>
            {
                int triCount = mesh.GetTriangleCount();
                for (int t = 0; t < triCount; t++)
                {
                    var tri = mesh.GetTriangle(t);
                    var n   = mesh.GetTriangleNormal(t);
                    var v0  = mesh.GetVertex(tri.X);
                    var v1  = mesh.GetVertex(tri.Y);
                    var v2  = mesh.GetVertex(tri.Z);
                    var centroid = new STTypes.TST3DPoint
                    {
                        X = (v0.X + v1.X + v2.X) / 3.0,
                        Y = (v0.Y + v1.Y + v2.Y) / 3.0,
                        Z = (v0.Z + v1.Z + v2.Z) / 3.0,
                    };
                    triangles.Add(new TriangleSample(centroid, n, TriangleArea(v0, v1, v2)));
                }
            });
        }
        return triangles;
    }

    private static double TriangleArea(STTypes.TST3DPoint a, STTypes.TST3DPoint b, STTypes.TST3DPoint c)
    {
        double ux = b.X - a.X, uy = b.Y - a.Y, uz = b.Z - a.Z;
        double vx = c.X - a.X, vy = c.Y - a.Y, vz = c.Z - a.Z;
        double cx = uy * vz - uz * vy;
        double cy = uz * vx - ux * vz;
        double cz = ux * vy - uy * vx;
        return 0.5 * Math.Sqrt(cx * cx + cy * cy + cz * cz);
    }

    private readonly record struct TriangleSample(
        STTypes.TST3DPoint Centroid,
        STTypes.TST3DPoint Normal,
        double Area);
}
