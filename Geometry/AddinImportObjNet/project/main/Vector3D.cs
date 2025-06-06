namespace AddinImportObjNet;

/// <summary>
/// Represents a 3D vector with X, Y, and Z coordinates
/// </summary>
public struct Vector3D
{
    /// <summary>
    /// X coordinate of the vector
    /// </summary>
    public readonly float X;
    
    /// <summary>
    /// Y coordinate of the vector
    /// </summary>
    public readonly float Y;
    
    /// <summary>
    /// Z coordinate of the vector
    /// </summary>
    public readonly float Z;
    
    /// <summary>
    /// Represents a 3D vector with X, Y, and Z coordinates
    /// </summary>
    public Vector3D(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}