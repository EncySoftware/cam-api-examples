using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using AptArcsNet.Model;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.TechOperation;

namespace AptArcsNet.Service;

/// <summary>
/// Drives the demo: adds a G-code based operation to the active project, loads the sample APT
/// program into it, calculates that one operation and reads back what the interpreter produced.
/// Every method here runs on an MTA worker thread — the window marshals the call.
/// </summary>
internal sealed class AptArcsService
{
    private const string SNCIFileProperty = "SNCIFile";

    /// <summary>
    /// An arc node of the toolpath carries its radius first and the arc centre as Xc/Yc/Zc:
    /// "R-10, X40, Y-20, Z-2, Xc30, Yc-20, Zc-2, Plane XY". A straight move has neither, it is the
    /// bare end point: "X-30, Y-30, Z-2". The words CIRCLE and ARC never appear in a caption.
    /// </summary>
    private static readonly Regex ArcCaption = new(@"^R(?<r>-?\d+(?:[.,]\d+)?)\b.*\bXc", RegexOptions.Compiled);

    private readonly ComWrapper<ICamApiApplication> _appCom;

    /// <param name="appCom">Live ENCY application, owned by the window</param>
    public AptArcsService(ComWrapper<ICamApiApplication> appCom)
    {
        _appCom = appCom;
    }

    /// <summary>Full path of the milling APT interpreter of this installation</summary>
    public string InterpreterPath()
    {
        using var pathsCom = _appCom.Paths();
        return Path.Combine(pathsCom.InterpretersFolder(), ExampleSettings.AptInterpreterRelativePath);
    }

    /// <summary>
    /// Adds the operation, loads the APT program and calculates just that operation, then reads
    /// its toolpath. The project and the machine already set up in ENCY are used as they are, and
    /// no other operation is touched — hence CalculateToolpath and not CalculateAllOperationsToolpath.
    /// </summary>
    public AptArcsReport Run()
    {
        if (!File.Exists(ExampleSettings.AptFilePath))
            throw new FileNotFoundException($"APT sample not found next to the plugin: {ExampleSettings.AptFilePath}");

        using var projectCom = _appCom.GetActiveProject();
        using var technologistCom = projectCom.Technologist();
        using var rootCom = technologistCom.RootOperation();

        var operationCom = technologistCom.CreateOperation(ExampleSettings.GCodeOperationType, rootCom.Id(), "");
        try
        {
            operationCom.SetName("APT arcs sample");
            WriteInterpreterPathForReference(operationCom);

            using (var gcodeOperationCom = operationCom.AsWithGCode())
            {
                if (gcodeOperationCom.IsNull)
                    throw new Exception($"{ExampleSettings.GCodeOperationType} does not answer ICamApiTechOperationWithGCode");
                gcodeOperationCom.LoadNCProgramFile(ExampleSettings.AptFilePath);
            }

            technologistCom.SetCurrentOperation(operationCom);
            technologistCom.CalculateToolpath(false);

            return ReadReport(operationCom);
        }
        finally
        {
            operationCom.Dispose();
        }
    }

    /// <summary>
    /// Writes the interpreter path into the operation so a saved project says which interpreter the
    /// example meant. It does NOT choose the interpreter for the calculation: the field the
    /// calculation reads is only refreshed inside LoadFromXMLProp, which the UI calls after the
    /// inspector is edited and the API never calls. What the calculation uses is whatever ENCY
    /// already has in hand — see the README.
    /// </summary>
    private void WriteInterpreterPathForReference(ComWrapper<ICamApiTechOperation> operationCom)
    {
        using var xmlPropCom = operationCom.XMLProp();
        xmlPropCom.SetStr(SNCIFileProperty, InterpreterPath());
    }

    /// <summary>
    /// Reads the calculated toolpath twice over: the node tree for what each move is, and the
    /// operation's own block statistics for the arc count ENCY reports without any parsing.
    /// </summary>
    private static AptArcsReport ReadReport(ComWrapper<ICamApiTechOperation> operationCom)
    {
        using var mcdTreeCom = operationCom.McdTree();
        if (mcdTreeCom.IsNull)
            throw new Exception("The operation has no toolpath after the calculation");

        using var iteratorCom = mcdTreeCom.GetNodes();
        var nodes = new List<ToolpathNode>();
        foreach (var nodeCom in iteratorCom.AsEnumerable())
            nodes.Add(ToNode(nodes.Count + 1, nodeCom.Caption()));

        var blocks = operationCom.GetBlocksStatistics();
        return new AptArcsReport(nodes, blocks.Arcs, blocks.Lines + blocks.MultiGoTo);
    }

    private static ToolpathNode ToNode(int index, string caption)
    {
        var match = ArcCaption.Match(caption);
        if (match.Success)
        {
            // the sign of the radius is the direction the arc is travelled, the demo only counts radii
            var radius = double.Parse(match.Groups["r"].Value.Replace(',', '.'), CultureInfo.InvariantCulture);
            return new ToolpathNode(index, ToolpathNodeKind.Arc, caption, Math.Abs(radius));
        }

        var kind = caption.StartsWith('X') ? ToolpathNodeKind.Line : ToolpathNodeKind.Other;
        return new ToolpathNode(index, kind, caption, null);
    }
}
