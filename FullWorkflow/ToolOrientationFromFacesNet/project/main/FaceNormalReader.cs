using CAMAPI.DotnetHelper;
using CAMAPI.Project;

using STTypes;

namespace ToolOrientationFromFacesNet;

/// <summary>
/// Collects the approach directions the operations are oriented by, reading them off the planar
/// faces of the imported part
/// </summary>
public static class FaceNormalReader
{
    /// <summary>
    /// Two normals closer than this are treated as the same direction
    /// </summary>
    private const double SameDirectionTolerance = 1e-6;

    /// <summary>
    /// Select the whole model and return the distinct normals of its planar faces
    /// </summary>
    /// <remarks>
    /// GetFaceListOfSelected builds the list out of selected FACE nodes. Selecting the part node
    /// itself - the one FindByFullName("Part\\&lt;file&gt;") returns - yields an empty list, which is why
    /// the example selects every node of the model instead.
    /// </remarks>
    public static List<TST3DPoint> ReadDistinctPlaneNormals(ComWrapper<ICamApiProject> projectCom)
    {
        using var geomModelCom = projectCom.CAMAPIGeomModel();
        geomModelCom.DeselectAll();

        // EnumerateNodes disposes every node it hands out, disposing it here as well would release it twice
        foreach (var nodeCom in geomModelCom.EnumerateNodes())
            nodeCom.SetSelected(true);

        using var facesCom = geomModelCom.GetFaceListOfSelected();
        var facesCount = facesCom.Invoke(faces => faces.Count);

        var normals = new List<TST3DPoint>();
        for (var i = 0; i < facesCount; i++)
        {
            using var faceCom = facesCom.InvokeAndWrap(faces => faces.Face[i]);

            // a non-planar face carries no single approach direction to take the tool axis from
            if (!faceCom.GetPlane(out _, out var normal))
                continue;

            if (!normals.Any(known => IsSameDirection(known, normal)))
                normals.Add(normal);
        }
        return normals;
    }

    /// <summary>
    /// Tell whether two unit normals point the same way
    /// </summary>
    private static bool IsSameDirection(TST3DPoint left, TST3DPoint right)
    {
        var dot = left.X * right.X + left.Y * right.Y + left.Z * right.Z;
        return Math.Abs(dot - 1.0) < SameDirectionTolerance;
    }
}
