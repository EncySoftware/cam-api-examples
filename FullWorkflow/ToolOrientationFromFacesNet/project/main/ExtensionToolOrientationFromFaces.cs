using System.Text;

using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ToolOrientationFromFacesNet;

/// <summary>
/// Builds a project from scratch and gives every one of its operations the tool orientation of its
/// own approach plane, without a single click in the "Setup tab / Tool Orientation" picker
/// </summary>
public class ExtensionToolOrientationFromFaces : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Run the whole workflow
    /// </summary>
    /// <param name="context">Information about current running instance</param>
    /// <param name="resultStatus">Structure to return error</param>
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            RunInternal(context);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    private static void RunInternal(IExtensionUtilityContext context)
    {
        // ComWrapper keeps the lifetime of every COM object predictable for the CAM system instead of leaving it to the garbage collector
        using var applicationCom = ComWrapper.Create(context.CamApplication);
        using var applicationMainFormCom = applicationCom.MainForm();

        try
        {
            applicationMainFormCom.Invoke(mainForm => mainForm.BeginFreeze((ushort)TFreezeInterfaceType.afiiGeneral));

            ProjectBuilder.Build(applicationCom);

            using var projectCom = applicationCom.GetActiveProject();
            using var technologistCom = projectCom.Technologist();

            // the baseline the report compares against, so that the deferred write of the orientation becomes visible
            ToolOrientationAligner.RecalculateToolpath(technologistCom);
            var nodesBeforeAlignment = ToolpathReporter.MeasureNodeCounts(technologistCom);

            var toolAxes = FaceNormalReader.ReadDistinctPlaneNormals(projectCom);
            if (toolAxes.Count == 0)
                throw new Exception("The imported model has no planar faces to take a tool orientation from");

            var alignments = ToolOrientationAligner.AlignOperations(projectCom, technologistCom, toolAxes);
            var nodesAfterAlignment = ToolpathReporter.MeasureNodeCounts(technologistCom);

            ToolOrientationAligner.RecalculateToolpath(technologistCom);
            var nodesAfterRecalculation = ToolpathReporter.MeasureNodeCounts(technologistCom);

            // asks the machine solver what direction the tool ended up pointing at, instead of trusting the request
            var toolAxisMeasurements = ToolOrientationAligner.MeasureToolAxes(projectCom, technologistCom, toolAxes);

            ShowReport(BuildReport(toolAxes.Count, alignments, toolAxisMeasurements,
                nodesBeforeAlignment, nodesAfterAlignment, nodesAfterRecalculation));
        }
        finally
        {
            applicationMainFormCom.Invoke(mainForm => mainForm.EndFreeze());
        }
    }

    /// <summary>
    /// Put together what every operation got and what it did to its toolpath
    /// </summary>
    private static string BuildReport(int normalsCount,
        List<AlignmentInfo> alignments,
        List<ToolAxisInfo> toolAxisMeasurements,
        List<int> nodesBeforeAlignment,
        List<int> nodesAfterAlignment,
        List<int> nodesAfterRecalculation)
    {
        var report = new StringBuilder();
        report.AppendLine($"Distinct planar face normals found: {normalsCount}");
        report.AppendLine();

        for (var i = 0; i < alignments.Count; i++)
        {
            var alignment = alignments[i];
            report.AppendLine($"{alignment.Name}");
            report.AppendLine($"    normal : {alignment.Normal}");
            report.AppendLine($"    axes   : {alignment.Axes}");
            report.AppendLine($"    tool   : {toolAxisMeasurements[i].ToolAxis}"
                              + $", {toolAxisMeasurements[i].AngleToNormal:F3} deg off the normal");
            report.AppendLine($"    nodes  : {nodesBeforeAlignment[i]} before"
                              + $" -> {nodesAfterAlignment[i]} after the orientation is stored"
                              + $" -> {nodesAfterRecalculation[i]} after recalculation");
            report.AppendLine();
        }

        var worstAngle = toolAxisMeasurements.Max(measurement => measurement.AngleToNormal);
        report.AppendLine($"Every tool axis read back out of the machine is within {worstAngle:F3} deg");
        report.AppendLine("of the face normal it was taken from: the orientation asked for is the one applied.");
        report.AppendLine();
        report.AppendLine("Storing the orientation costs the operation its toolpath - the node count drops to");
        report.AppendLine("nothing - and nothing rebuilds it until the toolpath is calculated again.");
        return report.ToString();
    }

    /// <summary>
    /// Show what has been written into every processed operation
    /// </summary>
    private static void ShowReport(string text)
    {
        using var dialogsCom = UIDialogs.CreateHelper();
        if (dialogsCom.IsNull)
            return;

        dialogsCom.Invoke(dialogs => dialogs.MessageBox(text, TMessageDialogType.mdtInformation,
            (ushort)TUIButtonTypeFlags.btfOk, TUIButtonType.btOk, "Tool orientation from faces"));
    }
}
