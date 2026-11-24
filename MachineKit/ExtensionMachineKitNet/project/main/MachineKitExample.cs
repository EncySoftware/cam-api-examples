using System.Globalization;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.MachineKit;

namespace ExtensionMachineKitNet;

/// <summary>
/// Creates a .mKit file with a connection, a sleeve, a conical mill and a custom mill from a STEP model, saves it and
/// reads it back
/// </summary>
public static class MachineKitExample
{
    /// <summary>
    /// Runs the whole example. Throws InvalidOperationException when a MachineKit call fails
    /// </summary>
    /// <param name="applicationCom">CAM application</param>
    /// <param name="kitPath">Path of the .mKit file to create; its folder is created, an old file is replaced</param>
    /// <param name="modelPath">STEP model of the custom mill</param>
    /// <param name="output">Where to write the report</param>
    public static void Run(ComWrapper<ICamApiApplication> applicationCom, string kitPath, string modelPath,
        TextWriter output)
    {
        using var manager = applicationCom.GetMachineKitManager();
        WriteSupportedTypes(manager, output);
        CreateKit(manager, kitPath, modelPath, output);
        WriteKit(manager, kitPath, output);
    }

    /// <summary>
    /// Writes the component and connection types the manager can create
    /// </summary>
    private static void WriteSupportedTypes(ComWrapper<ICamApiMachineKitManager> manager, TextWriter output)
    {
        var componentTypes = manager.GetSupportedComponentTypes();
        var connectionTypes = manager.GetSupportedConnectionTypes();
        output.WriteLine($"Supported component types ({componentTypes.Count}): {string.Join(", ", componentTypes)}");
        output.WriteLine($"Supported connection types ({connectionTypes.Count}): {string.Join(", ", connectionTypes)}");
        output.WriteLine();
    }

    /// <summary>
    /// Creates the kit, fills it and saves it
    /// </summary>
    private static void CreateKit(ComWrapper<ICamApiMachineKitManager> manager, string kitPath, string modelPath,
        TextWriter output)
    {
        // Existing folder and no file at the path, as CreateKit expects
        Directory.CreateDirectory(Path.GetDirectoryName(kitPath)!);
        if (File.Exists(kitPath))
            File.Delete(kitPath);

        using var kit = manager.CreateKit(kitPath);
        var shank = CreateShank(kit);
        CreateSleeve(kit, shank);
        CreateConicalMill(kit);
        CreateMillFromModel(kit, modelPath, output);
        kit.Save();
        kit.Close();
        output.WriteLine($"Saved: {kitPath}");
        output.WriteLine();
    }

    /// <summary>
    /// Creates a cylindrical connection of diameter 32 mm
    /// </summary>
    /// <returns>ID of the connection</returns>
    private static string CreateShank(ComWrapper<ICamApiMachineKit> kit)
    {
        var shank = kit.CreateConnection(TMachineKitConnectionType.mktCylinder, "Shank D32");
        kit.SetParameter(shank, "Diameter", 32);
        kit.SetConnectionCode(shank, "CYL-32");
        return shank;
    }

    /// <summary>
    /// Creates a sleeve and connects its plug and its first socket to the shank
    /// </summary>
    private static void CreateSleeve(ComWrapper<ICamApiMachineKit> kit, string shank)
    {
        var sleeve = kit.CreateComponent(TMachineKitComponentType.mkctSleeve, "Sleeve D32");

        // Order matters: each value is checked against the constraints of the sleeve
        kit.SetParameter(sleeve, "BarLength", 50);
        kit.SetParameter(sleeve, "TotalLength", 80);
        kit.SetParameter(sleeve, "FlangeDiameter", 40);
        kit.SetParameter(sleeve, "Diameter", 32);
        kit.SetParameter(sleeve, "HoleDiameter", 20);

        kit.SetPlug(sleeve, shank);
        var socket = kit.GetSockets(sleeve)[0];
        kit.SetSocket(sleeve, socket, shank);
    }

    /// <summary>
    /// Creates a conical mill; lengths are in mm, the angle is in degrees
    /// </summary>
    private static void CreateConicalMill(ComWrapper<ICamApiMachineKit> kit)
    {
        var mill = kit.CreateComponent(TMachineKitComponentType.mkctConicalMill, "Conical mill D12");
        kit.SetParameter(mill, "Length", 60);
        kit.SetParameter(mill, "ShoulderLength", 30);
        kit.SetParameter(mill, "WorkingLength", 20);
        kit.SetParameter(mill, "Diameter", 12);
        kit.SetParameter(mill, "Angle", 10);
    }

    /// <summary>
    /// Creates a custom mill from a STEP model and recognizes its cutting part. A failed import is reported, not
    /// thrown
    /// </summary>
    private static void CreateMillFromModel(ComWrapper<ICamApiMachineKit> kit, string modelPath, TextWriter output)
    {
        var created = kit.TryCreateComponentWithModel(TMachineKitComponentType.mkctCustomMillTool,
            "Custom mill from STEP", modelPath, true, out var mill, out var recognition, out var status);
        if (!created)
        {
            output.WriteLine($"Model is not imported: {status.Description}");
            return;
        }
        output.WriteLine($"Model imported: recognition {recognition}, cutting part {kit.GetToolSubtype(mill)}");
    }

    /// <summary>
    /// Opens the saved kit read-only and writes its connections and components
    /// </summary>
    private static void WriteKit(ComWrapper<ICamApiMachineKitManager> manager, string kitPath, TextWriter output)
    {
        using var kit = manager.OpenKit(kitPath);
        output.WriteLine($"Opened {kit.FileName()}, read-only: {kit.IsReadOnly()}");
        foreach (var connection in kit.GetConnections())
            WriteConnection(kit, connection, output);
        foreach (var component in kit.GetComponents())
            WriteComponent(kit, component, output);
        kit.Close();
    }

    /// <summary>
    /// Writes the name, type, code and parameters of a connection
    /// </summary>
    private static void WriteConnection(ComWrapper<ICamApiMachineKit> kit, string connection, TextWriter output)
    {
        output.WriteLine($"Connection \"{kit.GetName(connection)}\": {kit.GetConnectionType(connection)}, " +
                         $"code {kit.GetConnectionCode(connection)}");
        output.WriteLine($"  {FormatParameters(kit, connection)}");
    }

    /// <summary>
    /// Writes the name, type, plug, sockets and parameters of a component
    /// </summary>
    private static void WriteComponent(ComWrapper<ICamApiMachineKit> kit, string component, TextWriter output)
    {
        output.WriteLine($"Component \"{kit.GetName(component)}\": {kit.GetComponentType(component)}, " +
                         $"cutting part {kit.GetToolSubtype(component)}");
        var plug = kit.GetPlug(component);
        if (plug != "")
            output.WriteLine($"  plug: {kit.GetName(plug)}");
        foreach (var socket in kit.GetSockets(component))
        {
            var connections = kit.GetSocketConnections(component, socket).Select(kit.GetName);
            output.WriteLine($"  socket {socket}: {string.Join(", ", connections)}");
        }
        output.WriteLine($"  {FormatParameters(kit, component)}");
    }

    /// <summary>
    /// Numeric parameters of a component or connection as "key = value" pairs
    /// </summary>
    private static string FormatParameters(ComWrapper<ICamApiMachineKit> kit, string id)
    {
        var parameters = kit.GetParameters(id).Select(key =>
            $"{key} = {kit.GetParameter(id, key).ToString("0.###", CultureInfo.InvariantCulture)}");
        return string.Join("; ", parameters);
    }
}
