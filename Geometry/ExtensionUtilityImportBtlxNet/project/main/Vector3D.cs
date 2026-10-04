using STTypes;

namespace ExtensionUtilityImportBtlxNet;

/// <summary>
/// Point or direction in space
/// </summary>
public readonly record struct Vector3D(double X, double Y, double Z)
{
    /// <summary>
    /// Length of the vector
    /// </summary>
    public double Length => Math.Sqrt(Dot(this));

    /// <summary>
    /// Sum of two vectors
    /// </summary>
    public static Vector3D operator +(Vector3D left, Vector3D right)
        => new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    /// <summary>
    /// Difference of two vectors
    /// </summary>
    public static Vector3D operator -(Vector3D left, Vector3D right)
        => new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    /// <summary>
    /// Vector scaled by a factor
    /// </summary>
    public static Vector3D operator *(Vector3D vector, double factor)
        => new(vector.X * factor, vector.Y * factor, vector.Z * factor);

    /// <summary>
    /// Scalar product
    /// </summary>
    public double Dot(Vector3D other) => X * other.X + Y * other.Y + Z * other.Z;

    /// <summary>
    /// Vector product
    /// </summary>
    public Vector3D Cross(Vector3D other) => new(
        Y * other.Z - Z * other.Y,
        Z * other.X - X * other.Z,
        X * other.Y - Y * other.X);

    /// <summary>
    /// Vector of the same direction and unit length
    /// </summary>
    public Vector3D Normalized()
    {
        var length = Length;
        if (length < 1e-12)
            throw new Exception("Cannot normalize a zero vector");
        return this * (1 / length);
    }

    /// <summary>
    /// The same vector as the CAM system takes it
    /// </summary>
    public TST3DPoint ToPoint() => new() { X = X, Y = Y, Z = Z };
}

/// <summary>
/// Right-handed coordinate system given by its origin and two axes, the third one follows from them
/// </summary>
public readonly record struct Frame3D(Vector3D Origin, Vector3D XAxis, Vector3D YAxis)
{
    /// <summary>
    /// Third axis of the coordinate system
    /// </summary>
    public Vector3D ZAxis => XAxis.Cross(YAxis);

    /// <summary>
    /// Coordinate system with unit, mutually perpendicular axes, built from axes that are only roughly so
    /// </summary>
    public static Frame3D Orthonormal(Vector3D origin, Vector3D xAxis, Vector3D yAxis)
    {
        var x = xAxis.Normalized();
        var y = (yAxis - x * yAxis.Dot(x)).Normalized();
        return new Frame3D(origin, x, y);
    }

    /// <summary>
    /// Point given in this coordinate system, expressed in the parent one
    /// </summary>
    public Vector3D PointToParent(Vector3D point)
        => Origin + DirectionToParent(point);

    /// <summary>
    /// Direction given in this coordinate system, expressed in the parent one
    /// </summary>
    public Vector3D DirectionToParent(Vector3D direction)
        => XAxis * direction.X + YAxis * direction.Y + ZAxis * direction.Z;

}
