namespace AddinImportObjNet;

/// <summary>
/// Coordinates for texture mapping in 3D models
/// </summary>
public struct TextureCoordinate
{
    /// <summary>
    /// U coordinate for texture mapping
    /// </summary>
    public float U;
    
    /// <summary>
    /// V coordinate for texture mapping
    /// </summary>
    public float V;
    
    /// <summary>
    /// Coordinates for texture mapping in 3D models
    /// </summary>
    public TextureCoordinate(float u, float v)
    {
        U = u;
        V = v;
    }
}