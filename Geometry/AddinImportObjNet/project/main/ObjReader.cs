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
    public void BuildModel(ISTGeomReceiver geomReceiver)
    {
        BuildMesh(geomReceiver);
        BuildFaces(geomReceiver);
    }

    private void BuildFaces(ISTGeomReceiver geomReceiver)
    {
        foreach (var face in _model.Faces)
        {
            if (face.VertexIndices.Count < 2)
                continue;

            var isClosed = face.VertexIndices.Count > 2;
            const string contourName = "mesher";
            geomReceiver.StartCurve3d(contourName, GetVertex(face.VertexIndices[0].VertexIndex));

            for (var i = 1; i < face.VertexIndices.Count; i++)
                geomReceiver.CutTo3d(GetVertex(face.VertexIndices[i].VertexIndex));

            if (isClosed)
                geomReceiver.CutTo3d(GetVertex(face.VertexIndices[0].VertexIndex));

            geomReceiver.CloseCurve3d(isClosed);
            geomReceiver.AddEntity(contourName, isClosed ? "face" : "edge");
        }
    }

    private void BuildMesh(ISTGeomReceiver geomReceiver)
    {
        geomReceiver.StartMesh("model_mesh");

        for (var i = 0; i < _model.Vertices.Count; i++)
            geomReceiver.AddMeshVertex(i, GetVertex(i));

        foreach (var face in _model.Faces)
        {
            if (face.VertexIndices.Count < 3)
                continue;

            for (var i = 2; i < face.VertexIndices.Count; i++)
            {
                geomReceiver.AddMeshTriangle(
                    face.VertexIndices[0].VertexIndex,
                    face.VertexIndices[i - 1].VertexIndex,
                    face.VertexIndices[i].VertexIndex
                );
            }
        }

        geomReceiver.CloseMesh();
        geomReceiver.AddEntity("model_mesh", "brick");
    }

    private TST3DPoint GetVertex(int vertexIndex) =>
        new()
        {
            X = _model.Vertices[vertexIndex].X,
            Y = _model.Vertices[vertexIndex].Y,
            Z = _model.Vertices[vertexIndex].Z
        };
}