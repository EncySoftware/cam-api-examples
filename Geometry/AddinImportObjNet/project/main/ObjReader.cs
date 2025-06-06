using STGeomApiTypes;
using STTypes;

namespace AddinImportObjNet;

/// <summary>
/// Reads an OBJ file and builds a 3D model using the provided geometry filer
/// </summary>
public class ObjReader
{
    private readonly ObjModel _model;

    /// <summary>
    /// Reads an OBJ file and builds a 3D model using the provided geometry filer
    /// </summary>
    public ObjReader(string inputFile)
    {
        _model = ObjDataParser.Parse(inputFile);
    }

    /// <summary>
    /// Reads an OBJ file and builds a 3D model using the provided geometry filer
    /// </summary>
    public void BuildModel(ISTGeomFiler geomFile)
    {
        BuildMesh(geomFile);
        BuildFaces(geomFile);
    }

    private void BuildFaces(ISTGeomFiler geomFile)
    {
        foreach (var face in _model.Faces)
        {
            if (face.VertexIndices.Count < 2)
                continue;

            var isClosed = face.VertexIndices.Count > 2;
            const string contourName = "mesher";
            geomFile.StartCurve3d(contourName, GetVertex(face.VertexIndices[0].VertexIndex));

            for (var i = 1; i < face.VertexIndices.Count; i++)
                geomFile.CutTo3d(GetVertex(face.VertexIndices[i].VertexIndex));

            if (isClosed)
                geomFile.CutTo3d(GetVertex(face.VertexIndices[0].VertexIndex));

            geomFile.CloseCurve3d(isClosed);
            geomFile.AddEntity(contourName, isClosed ? "face" : "edge");
        }
    }

    private void BuildMesh(ISTGeomFiler geomFile)
    {
        geomFile.StartMesh("model_mesh");

        for (var i = 0; i < _model.Vertices.Count; i++)
            geomFile.AddMeshVertex(i, GetVertex(i));

        foreach (var face in _model.Faces)
        {
            if (face.VertexIndices.Count < 3)
                continue;

            for (var i = 2; i < face.VertexIndices.Count; i++)
            {
                geomFile.AddMeshTriangle(
                    face.VertexIndices[0].VertexIndex,
                    face.VertexIndices[i - 1].VertexIndex,
                    face.VertexIndices[i].VertexIndex
                );
            }
        }

        geomFile.CloseMesh();
        geomFile.AddEntity("model_mesh", "brick");
    }

    private TST3DPoint GetVertex(int vertexIndex) =>
        new()
        {
            X = _model.Vertices[vertexIndex].X,
            Y = _model.Vertices[vertexIndex].Y,
            Z = _model.Vertices[vertexIndex].Z
        };
}