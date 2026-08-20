using CADAPI.DotnetHelper;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace CadSketchDrawingNet;

/// <summary>
/// Utility that builds a ring-flange gasket outline with the CAD API and
/// publishes it into the active project's geometry model. Demonstrates
/// concentric circles, a computed bolt-hole pattern and an arc slot.
/// </summary>
public class FlangeGasketSketchExample : IExtension, IExtensionUtility
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

        using var modelCom = projectCom.CAMAPIGeomModel();
        using var cadNodeCom = modelCom.AddCadGroup("Flange gasket");
        using var cadCom = cadNodeCom.AsCadModel();
        using var sketchCom = cadCom.AddSketch(3, 0);

        // Outer edge and central bore.
        sketchCom.AddCircle(0, 0, 60);
        sketchCom.AddCircle(0, 0, 30);

        // Six bolt holes evenly spaced on a 45 mm bolt circle.
        const int boltCount = 6;
        const double boltCircleRadius = 45;
        for (var i = 0; i < boltCount; i++)
        {
            var angle = 2 * Math.PI * i / boltCount;
            sketchCom.AddCircle(boltCircleRadius * Math.Cos(angle), boltCircleRadius * Math.Sin(angle), 4);
        }

        // Curved relief slot running along a 52 mm arc from 200 to 340 degrees.
        var startAngle = 200 * Math.PI / 180;
        var endAngle = 340 * Math.PI / 180;
        const double slotArcRadius = 52;
        sketchCom.AddArcSlot(
            slotArcRadius * Math.Cos(startAngle), slotArcRadius * Math.Sin(startAngle),
            slotArcRadius * Math.Cos(endAngle), slotArcRadius * Math.Sin(endAngle),
            0, 0, slotArcRadius, 3);

        cadCom.Save();
    }
}
