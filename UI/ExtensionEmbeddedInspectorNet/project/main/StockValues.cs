namespace ExtensionEmbeddedInspectorNet;

/// <summary>
/// Values the user edits in the inspector
/// </summary>
public class StockValues
{
    /// <summary>
    /// Materials offered in the enum property
    /// </summary>
    public static readonly string[] Materials = ["Steel", "Aluminium", "Plastic"];

    /// <summary>
    /// Name of the stock
    /// </summary>
    public string Caption { get; set; } = "Stock 1";

    /// <summary>
    /// Size along X, mm
    /// </summary>
    public double Length { get; set; } = 100;

    /// <summary>
    /// Size along Y, mm
    /// </summary>
    public double Width { get; set; } = 60;

    /// <summary>
    /// Size along Z, mm
    /// </summary>
    public double Height { get; set; } = 30;

    /// <summary>
    /// Index in <see cref="Materials"/>
    /// </summary>
    public int MaterialIndex { get; set; }

    /// <summary>
    /// Show the stock in the 3D view
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Independent copy of the values
    /// </summary>
    public StockValues Copy()
    {
        return (StockValues)MemberwiseClone();
    }

    /// <summary>
    /// One-line description of the values
    /// </summary>
    public override string ToString()
    {
        return $"{Caption}: {Length:0.###} x {Width:0.###} x {Height:0.###} mm, " +
               $"{Materials[MaterialIndex]}, {(Visible ? "visible" : "hidden")}";
    }
}
