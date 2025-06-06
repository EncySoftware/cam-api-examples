namespace AddinImportObjNet;

/// <summary>
/// Information about a 3D model parsed from an .obj file
/// </summary>
public class ObjModel
{
    /// <summary>
    /// List of vertices in the model
    /// </summary>
    public List<Vector3D> Vertices { get; } = [];
    
    /// <summary>
    /// List of normals in the model
    /// </summary>
    public List<Vector3D> Normals { get; } = [];
    
    /// <summary>
    /// List of texture coordinates in the model
    /// </summary>
    public List<TextureCoordinate> TextureCoordinates { get; } = [];
    
    /// <summary>
    /// List of faces in the model
    /// </summary>
    public List<Face> Faces { get; } = [];
}