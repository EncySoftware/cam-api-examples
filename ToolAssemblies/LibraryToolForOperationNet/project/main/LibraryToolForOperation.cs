using System.Globalization;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;
using CAMAPI.TechOperation;
using CAMAPI.ToolAssemblies;

namespace LibraryToolForOperationNet;

/// <summary>
/// Finds tools D10 in the kit of the machine and in the enabled component libraries, writes their parameters and
/// installs the first mill among them as the tool of an operation
/// </summary>
public static class LibraryToolForOperation
{
    /// <summary>
    /// Diameter of the tool to find, mm
    /// </summary>
    public const double Diameter = 10;

    /// <summary>
    /// Allowed difference from <see cref="Diameter"/>, mm
    /// </summary>
    public const double DiameterTolerance = 0.1;

    /// <summary>
    /// Runs the whole example. Throws InvalidOperationException when the project or the operation cannot take a
    /// tool from a library, Exception with the message of ENCY when a call fails
    /// </summary>
    /// <param name="projectCom">Active project</param>
    /// <param name="operationCom">Operation to install the tool for</param>
    /// <param name="output">Where to write the report</param>
    public static void Run(ComWrapper<ICamApiProject> projectCom, ComWrapper<ICamApiTechOperation> operationCom,
        TextWriter output)
    {
        using var machineToolsCom = projectCom.MachineTools();
        if (machineToolsCom.IsNull)
            throw new InvalidOperationException(
                "The project is in the classic tools mode and has no machine tools. A tool from a component " +
                "library is installed only in a project created in the tool assemblies mode on a machine with a kit.");
        using var operationToolCom = operationCom.AsWithToolAssembly();
        if (operationToolCom.IsNull)
            throw new InvalidOperationException($"The operation \"{operationCom.Name()}\" has no tool assembly.");

        var found = machineToolsCom.FindTools(diameterMin: Diameter - DiameterTolerance,
            diameterMax: Diameter + DiameterTolerance);
        try
        {
            WriteFound(found, output);
            if (found.Count == 0)
            {
                output.WriteLine($"Enable a component library with tools D{Format(Diameter)} in the library window.");
                return;
            }
            var millCom = found.FirstOrDefault(c => c.ComponentKind() == TCamApiToolComponentKind.tckMillTool);
            if (millCom == null)
            {
                output.WriteLine("None of them is a mill: the tool of the operation stays as it is.");
                return;
            }
            Install(operationCom, operationToolCom, millCom, output);
        }
        finally
        {
            foreach (var componentCom in found)
                componentCom.Dispose();
        }
    }

    /// <summary>
    /// Writes the caption, library, overhang and numeric parameters of every found tool
    /// </summary>
    private static void WriteFound(IReadOnlyList<ComWrapper<ICamApiToolComponent>> found, TextWriter output)
    {
        output.WriteLine($"Tools D{Format(Diameter)} ±{Format(DiameterTolerance)} mm in the kit of the machine " +
                         $"and in the enabled libraries: {found.Count}");
        foreach (var componentCom in found)
        {
            var libraryPath = componentCom.Invoke(c => c.LibraryPath);
            output.WriteLine($"\"{componentCom.ComponentCaption()}\" from {libraryPath}");
            output.WriteLine($"  overhang {Format(componentCom.ComponentOverhang())}; {FormatParameters(componentCom)}");
        }
        output.WriteLine();
    }

    /// <summary>
    /// Numeric parameters of a component as "key = value" pairs
    /// </summary>
    private static string FormatParameters(ComWrapper<ICamApiToolComponent> componentCom)
    {
        var parameters = componentCom.ComponentParameters()
            .Select(key => $"{key} = {Format(componentCom.ComponentParameter(key))}");
        return string.Join("; ", parameters);
    }

    /// <summary>
    /// Builds an assembly for the mill and installs it as the tool of the operation, through an unknown adapter when
    /// the libraries have no chain of adapters to the spindle
    /// </summary>
    private static void Install(ComWrapper<ICamApiTechOperation> operationCom,
        ComWrapper<ICamApiTechOperationWithToolAssembly> operationToolCom, ComWrapper<ICamApiToolComponent> millCom,
        TextWriter output)
    {
        var caption = millCom.ComponentCaption();
        var nodeBefore = operationToolCom.Invoke(op => op.ToolNodeID);
        var nodeId = operationToolCom.InstallTool(millCom, allowUnknownAdapter: true);
        output.WriteLine($"\"{caption}\" is installed for the operation \"{operationCom.Name()}\" as tool node {nodeId}");
        output.WriteLine($"ToolNodeID of the operation: {nodeBefore} before, {operationToolCom.Invoke(op => op.ToolNodeID)} now");
    }

    private static string Format(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);
}
