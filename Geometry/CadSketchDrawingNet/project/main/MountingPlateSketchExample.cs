using CADAPI.DotnetHelper;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace CadSketchDrawingNet;

/// <summary>
/// Utility that builds a mounting-plate outline with the CAD API and publishes
/// it into the active project's geometry model. Demonstrates the sketch
/// primitives rectangle, circle, slot and polygon.
/// </summary>
public class MountingPlateSketchExample : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext Context, out TResultStatus resultStatus)
    {
        resultStatus = default;

        try
        {
            RunInternal(Context);
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    private static void RunInternal(IExtensionUtilityContext Context)
    {
        using var appCom = new ComWrapper<ICamApiApplication>(Context.CamApplication);
        using var projectCom = appCom.GetActiveProject();
        if (projectCom.IsNull)
            throw new Exception("No active project. Open or create a project before running this utility.");

        // AddCadGroup makes a CAD-typed tree node; AsCadModel resolves it; AddSketch opens a sketch on the XY plane (normal axis 3 = Z).
        using var modelCom = projectCom.CAMAPIGeomModel();
        using var cadNodeCom = modelCom.AddCadGroup("Mounting plate");
        using var cadCom = cadNodeCom.AsCadModel();
        using var sketchCom = cadCom.AddSketch(3, 0);

        // Plate outline: 120 x 80 mm, centred on the origin.
        sketchCom.AddRectangleFromCenter(0, 0, 120, 80);

        // Four mounting holes, 12 mm in from each edge.
        sketchCom.AddCircle(48, 28, 4);
        sketchCom.AddCircle(-48, 28, 4);
        sketchCom.AddCircle(48, -28, 4);
        sketchCom.AddCircle(-48, -28, 4);

        // Cable slot across the lower half.
        sketchCom.AddSlot(-25, -12, 25, -12, 5);

        // Hex access cut-out in the upper half.
        sketchCom.AddPolygon(0, 14, 9, 6);

        // Publish the sketch as a child tree node so the viewport shows it.
        cadCom.Save();
    }
}
