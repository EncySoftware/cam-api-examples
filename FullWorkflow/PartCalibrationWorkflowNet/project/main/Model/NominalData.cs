namespace PartCalibrationWorkflowNet.Model;

/// <summary>Root object for nominal.json.</summary>
public class NominalData
{
    /// <summary>Source model file name used when the project was created.</summary>
    public string ModelFile { get; set; } = "";

    /// <summary>Nominal probe points in model space with surface normals.</summary>
    public List<NominalPoint> Points { get; set; } = new();
}
