using System.Reflection;

namespace ToolOrientationFromFacesNet;

/// <summary>
/// Everything the example works with. All the assets ship next to the extension itself, so the
/// example does not depend on what is installed on the machine that runs it
/// </summary>
public static class ExampleSettings
{
    /// <summary>
    /// Folder this DLL is loaded from, every asset below is resolved against it. Taken from the
    /// assembly and not from AppContext.BaseDirectory, which points at the CAM system itself
    /// </summary>
    private static string ExtensionFolder =>
        Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
        ?? throw new Exception("Cannot resolve the folder the extension is loaded from");

    /// <summary>
    /// Part to import. The model has to carry planar faces looking in different directions,
    /// otherwise every operation ends up with the same orientation and the example shows nothing.
    /// This one has 36 faces, 6 distinct planes, 4 of them tilted
    /// </summary>
    public static string ImportFilePath => Path.Combine(ExtensionFolder, @"assets\part\Part.IGS");

    /// <summary>
    /// Machine the example works on, taken from the machines shipped with the CAM system. A robot
    /// with an external positioner is the case this example exists for: it is where the inverse
    /// kinematics branches and where the tool orientation is hardest to set
    /// </summary>
    /// <remarks>
    /// FindMachine resolves this out of the installed library, so neither the schema nor the 3D
    /// models of the robot have to ship with the example - and the robot is drawn in the viewport.
    /// </remarks>
    public static readonly Guid MachineGuid = Guid.Parse("05bb597f-8fde-4aac-b943-84fe6e5caf70");

    /// <summary>
    /// Machine type inside the schema file
    /// </summary>
    public const string MachineTypeName = "Kuka_KR150_180_210_240-2";

    /// <summary>
    /// Connector of the machine the part is mounted on. On this robot the plate of the E1/E2
    /// positioner, whose rotary axes then take part in reaching the tool orientation
    /// </summary>
    public const string WorkpieceConnectorName = "Workpiece";

    /// <summary>
    /// Type of the operations the example creates
    /// </summary>
    public const string OperationTypeId = "TSTFaceMillingOp";

    /// <summary>
    /// How many operations to create and orient. The customer case behind this example runs 20+,
    /// the loop does not change with the count
    /// </summary>
    public const int OperationsCount = 3;
}
