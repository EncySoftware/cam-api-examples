namespace AddinImportObjNet;

/// <summary>
/// List of vertex indices that make up the face
/// </summary>
public struct VertexIndices
{
    /// <summary>
    /// Index of the vertex in the model's vertex list
    /// </summary>
    public int VertexIndex;
    
    /// <summary>
    /// Index of the texture coordinate in the model's texture coordinate list
    /// </summary>
    public int? TextureCoordIndex;
    
    /// <summary>
    /// Index of the normal vector in the model's normal list
    /// </summary>
    public int? NormalIndex;
}