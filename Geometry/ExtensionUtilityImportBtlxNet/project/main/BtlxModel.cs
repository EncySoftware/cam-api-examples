namespace ExtensionUtilityImportBtlxNet;

/// <summary>
/// One timber part of a BTLx file: a beam of Length x Height x Width
/// </summary>
/// <param name="Name">Name the part gets in the geometry tree</param>
/// <param name="Length">Size along the X axis of the part</param>
/// <param name="Height">Size along the Y axis of the part</param>
/// <param name="Width">Size along the Z axis of the part</param>
/// <param name="Placement">Position of the part in the project, null when the file does not give one</param>
public record BtlxPart(
    string Name,
    double Length,
    double Height,
    double Width,
    Frame3D? Placement);
