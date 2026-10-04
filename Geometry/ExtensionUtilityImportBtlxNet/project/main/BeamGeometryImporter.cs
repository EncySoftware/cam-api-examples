using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.Singletons;
using STGeomApiTypes;

namespace ExtensionUtilityImportBtlxNet;

/// <summary>
/// Part of a BTLx file together with the position it takes in the project
/// </summary>
/// <param name="Part">Part as the file describes it</param>
/// <param name="Frame">Coordinate system of the part in the project</param>
public record PlacedPart(BtlxPart Part, Frame3D Frame);

/// <summary>
/// Puts every part of a BTLx file into the geometry tree as a mesh of a beam
/// </summary>
public static class BeamGeometryImporter
{
    private const string GeomFileSingletonId = "Extension.Global.Singletons.GeomFile";

    /// <summary>
    /// Gap between the parts that carry no position of their own
    /// </summary>
    private const double GapBetweenParts = 50;

    /// <summary>
    /// Triangles of a beam over the corners <see cref="Corners"/> gives, two per side
    /// </summary>
    private static readonly int[][] Triangles =
    [
        [0, 2, 3], [0, 3, 1],
        [4, 5, 7], [4, 7, 6],
        [0, 1, 5], [0, 5, 4],
        [2, 6, 7], [2, 7, 3],
        [0, 4, 6], [0, 6, 2],
        [1, 3, 7], [1, 7, 5]
    ];

    /// <summary>
    /// Give every part its position in the project: the one the file carries, or a place in a row
    /// of beams lying side by side with their reference side 3 up
    /// </summary>
    public static List<PlacedPart> Place(IReadOnlyList<BtlxPart> parts)
    {
        var placed = new List<PlacedPart>();
        var rowOffset = 0.0;

        foreach (var part in parts)
        {
            if (part.Placement != null)
            {
                placed.Add(new PlacedPart(part, part.Placement.Value));
                continue;
            }

            // the height of the part looks up, so its width runs against the Y axis of the project
            var frame = new Frame3D(new Vector3D(0, -rowOffset, 0), new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
            placed.Add(new PlacedPart(part, frame));
            rowOffset += part.Width + GapBetweenParts;
        }
        return placed;
    }

    /// <summary>
    /// Write the beams into a geometry file and import it into the project
    /// </summary>
    public static void Import(ComWrapper<ICamApiProject> projectCom, IReadOnlyList<PlacedPart> parts, string groupName)
    {
        var sgfFilePath = Path.Combine(Path.GetTempPath(), $"btlx_{Guid.NewGuid():N}.sgf");
        try
        {
            WriteGeometryFile(sgfFilePath, parts, groupName);

            using var importerCom = projectCom.GeomImporter();
            importerCom.ImportFile(sgfFilePath, "", false);
        }
        finally
        {
            File.Delete(sgfFilePath);
        }
    }

    private static void WriteGeometryFile(string sgfFilePath, IReadOnlyList<PlacedPart> parts, string groupName)
    {
        using var factoryCom = SystemExtensionFactory.GetSingletonExtension<ICamApiFactoryGeometryFile>(GeomFileSingletonId);
        if (factoryCom.IsNull)
            throw new Exception("Can't get geometry filer singleton");

        using var geomFileCom = factoryCom.InvokeAndWrap(factory => factory.CreateObject());
        if (geomFileCom.IsNull)
            throw new Exception("Can't create geometry filer object");

        geomFileCom.Invoke(geomFile =>
        {
            if (!geomFile.StartFile(sgfFilePath))
                throw new Exception("Can't start file: " + sgfFilePath);
            try
            {
                if (geomFile is not ISTGeomReceiver geomReceiver)
                    throw new Exception("Can't cast geometry filer to geometry receiver");

                WriteModel(geomReceiver, parts, groupName);
            }
            finally
            {
                geomFile.CloseFile();
            }
        });
    }

    private static void WriteModel(ISTGeomReceiver geomReceiver, IReadOnlyList<PlacedPart> parts, string groupName)
    {
        geomReceiver.StartModel();
        try
        {
            // the corners are written in project coordinates already, so the meshes need no transform of their own
            var unit = new Frame3D(new Vector3D(0, 0, 0), new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
            geomReceiver.SetCurrentTransform(unit.Origin.ToPoint(), unit.ZAxis.ToPoint(), unit.XAxis.ToPoint());

            geomReceiver.StartGroupEntity(groupName);
            try
            {
                foreach (var part in parts)
                    AddBeam(geomReceiver, part);
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

    private static void AddBeam(ISTGeomReceiver geomReceiver, PlacedPart part)
    {
        var name = part.Part.Name;
        geomReceiver.StartMesh(name);

        var corners = Corners(part.Part);
        for (var index = 0; index < corners.Count; index++)
            geomReceiver.AddMeshVertex(index, part.Frame.PointToParent(corners[index]).ToPoint());

        foreach (var triangle in Triangles)
            geomReceiver.AddMeshTriangle(triangle[0], triangle[1], triangle[2]);

        geomReceiver.CloseMesh();
        geomReceiver.AddEntity(name, name);
    }

    /// <summary>
    /// Eight corners of the part in its own coordinate system. Bits 0, 1 and 2 of the index tell
    /// whether the corner sits at the far end along X, Y and Z
    /// </summary>
    private static List<Vector3D> Corners(BtlxPart part)
    {
        var corners = new List<Vector3D>();
        for (var index = 0; index < 8; index++)
            corners.Add(new Vector3D(
                (index & 1) != 0 ? part.Length : 0,
                (index & 2) != 0 ? part.Height : 0,
                (index & 4) != 0 ? part.Width : 0));
        return corners;
    }
}
