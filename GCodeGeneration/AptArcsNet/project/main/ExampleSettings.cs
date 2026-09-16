using System.IO;
using System.Reflection;

namespace AptArcsNet;

/// <summary>
/// Everything the example works with. The APT program ships next to the extension itself, so the
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
    /// APT program to interpret. Its 8 arcs are written as CIRCLE records — the R10 corners of an
    /// 80x60 contour and a full R20 circle built from four 90-degree arcs
    /// </summary>
    public static string AptFilePath => Path.Combine(ExtensionFolder, @"assets\ARC_SAMPLE.APT");

    /// <summary>
    /// Operation type the example creates: the milling operation driven by an existing NC program
    /// instead of by geometry. It is the only operation type that runs an interpreter
    /// </summary>
    public const string GCodeOperationType = "TNCMillOp";

    /// <summary>
    /// Milling APT interpreter, relative to <c>ICamApiPaths.InterpretersFolder</c>. The one the
    /// calculation actually uses is whatever ENCY has in hand — see the README
    /// </summary>
    public const string AptInterpreterRelativePath = @"Mill\Apt_Mill.snci";
}
