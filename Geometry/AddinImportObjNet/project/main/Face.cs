namespace AddinImportObjNet;

/// <summary>
/// Face in a 3D model, consisting of vertices, texture coordinates, and normals
/// </summary>
public class Face
{
    /// <summary>
    /// List of vertex indices that make up the face
    /// </summary>
    public List<VertexIndices> VertexIndices { get; } = [];
}