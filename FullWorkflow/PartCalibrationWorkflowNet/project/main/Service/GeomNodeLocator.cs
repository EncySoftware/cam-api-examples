using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.Project;
using PartCalibrationWorkflowNet.Model;
using STTypes;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Walks the project geometry tree to find nodes by full path and to extract
/// point coordinates from a sub-tree. Used by Tabs 2 / 4 / 5 when the user
/// references a folder by name.
/// </summary>
internal static class GeomNodeLocator
{
    /// <summary>
    /// Collect all point nodes whose ancestry contains a node with the given
    /// <paramref name="fullName"/>. Returns an empty list if the folder is not
    /// found or empty.
    /// </summary>
    public static List<Point3D> ReadPointsFromFolder(
        ComWrapper<ICamApiProject> projCom,
        string fullName)
    {
        using var geomCom = projCom.CAMAPIGeomModel();
        var result = new List<Point3D>();
        var debugSeen = new List<string>();

        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            var nodeFullName = nodeCom.FullName();

            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != TCAMAPIGeometryEntityType.etPoint) continue;
            debugSeen.Add(nodeFullName);

            // Match if the requested folder name appears anywhere in the
            // point's full path — the SGF importer prefixes the group with
            // implementation-specific parents (e.g. "Job/Geometry/<group>")
            // that the user does not usually type into the field.
            if (fullName.Length > 0 &&
                nodeFullName.IndexOf(fullName, StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            // GetBoundBox with identity matrix yields the point coordinates
            // for a single-point entity (min == max).
            var identity = new TST3DMatrix
            {
                vX = new TST3DPoint { X = 1, Y = 0, Z = 0 },
                vY = new TST3DPoint { X = 0, Y = 1, Z = 0 },
                vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
                vT = default,
                D  = 1
            };
            var box = entityCom.Invoke(e => e.GetBoundBox(identity, out _));
            result.Add(new Point3D
            {
                Name = nodeCom.Name(),
                X = box.Min.X, Y = box.Min.Y, Z = box.Min.Z,
            });
        }
        if (result.Count == 0)
            DbgLog.Write($"ReadPointsFromFolder('{fullName}'): no match, " +
                         $"seen {debugSeen.Count} etPoint nodes — " +
                         string.Join(", ", debugSeen.Take(5)) +
                         (debugSeen.Count > 5 ? ", ..." : ""));
        return result;
    }

    /// <summary>
    /// Find the first node by exact full-name match (case-insensitive).
    /// Returns null wrapper if not found.
    /// </summary>
    public static ComWrapper<ICAMAPIGeometryTreeNode>? FindByFullName(
        ComWrapper<ICamApiProject> projCom,
        string fullName)
    {
        using var geomCom = projCom.CAMAPIGeomModel();
        return geomCom.InvokeAndWrap(model => model.FindByFullName(fullName, out _));
    }

    /// <summary>
    /// Full names of every group (folder) node in the project geometry tree.
    /// Same set the geometry picker offered (it filtered to etfGroup), but as
    /// plain data — no host UI form, no main-thread marshalling — so it can
    /// populate a combo box directly.
    /// </summary>
    public static List<string> ListGroupFullNames(ComWrapper<ICamApiProject> projCom)
    {
        using var geomCom = projCom.CAMAPIGeomModel();
        var result = new List<string>();
        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != TCAMAPIGeometryEntityType.etGroup) continue;
            result.Add(nodeCom.FullName());
        }
        return result;
    }
}
