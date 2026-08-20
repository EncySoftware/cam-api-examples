using CAMAPI.DotnetHelper;
using CAMAPI.GeomModel;
using CAMAPI.ModelFormerTypes;
using CAMAPI.Project;
using CAMAPI.TechOperation;
using CAMAPI.Technologist;
using STTypes;

namespace WorkpieceMillingWorkflowNet;

/// <summary>
/// Mounts the workpiece on the machine and builds the stock and the fixtures through the model former API,
/// without importing any stock geometry
/// </summary>
public static class WorkpieceHelper
{
    /// <summary>
    /// Extra material left around the part on every side of the box stock, in mm
    /// </summary>
    private const double StockAllowance = 0.0;

    /// <summary>
    /// Placement of the workpiece on its connector: raised above the machine table by the height of the vise
    /// </summary>
    private static readonly TST3DMatrix WorkpieceOffset = new()
    {
        vX = new TST3DPoint { X = 1, Y = 0, Z = 0 },
        vY = new TST3DPoint { X = 0, Y = 1, Z = 0 },
        vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
        vT = new TST3DPoint { X = 0, Y = 0.234, Z = 105 }
    };

    /// <summary>
    /// Name of the coordinate system the workpiece is mounted by
    /// </summary>
    private const string WorkpieceCoordinateSystemName = "Global CS";

    /// <summary>
    /// Placement of the vise on its connector: shifted under the part and turned by -90 degrees around Z
    /// </summary>
    private static readonly TST3DMatrix ViseSetupLcs = new()
    {
        vX = new TST3DPoint { X = 0, Y = -1, Z = 0 },
        vY = new TST3DPoint { X = 1, Y = 0, Z = 0 },
        vZ = new TST3DPoint { X = 0, Y = 0, Z = 1 },
        vT = new TST3DPoint { X = 0, Y = 5, Z = -105 }
    };

    /// <summary>
    /// Name of the vise package in the fixtures library, without the .mcp extension. The caption stored
    /// inside the package is spelled with a Cyrillic character, so the package is looked up by file name
    /// </summary>
    private const string ViseLibraryComponentName = "Vise 66x82";

    /// <summary>
    /// Distance the movable jaw is opened to, in mm
    /// </summary>
    private const double JawPosition = 63.25;

    /// <summary>
    /// Mount the workpiece on the given connector of the machine and raise it to the height of the vise
    /// </summary>
    public static void MountWorkpiece(ComWrapper<ICamApiTechnologist> technologistCom, int connectorIndex)
    {
        using var partAndStageListCom = technologistCom.PartAndStageList();
        using var partStageCom = partAndStageListCom.GetPartStage(0, 0);
        using var workpieceSetupCom = partStageCom.WorkpieceSetup();
        workpieceSetupCom.SetMachineSideConnectorIndex(connectorIndex);
        workpieceSetupCom.SetWorkpieceSideCoordinateSystemName(WorkpieceCoordinateSystemName);
        workpieceSetupCom.SetOffset(WorkpieceOffset);
    }

    /// <summary>
    /// Build a box stock around the part and clamp it in a vise
    /// </summary>
    public static void SetupStockAndFixtures(ComWrapper<ICamApiTechnologist> technologistCom,
        int baseTableConnectorIndex)
    {
        using var rootOperationCom = technologistCom.RootOperation();
        BuildBoxStock(rootOperationCom);
        AddVise(rootOperationCom, baseTableConnectorIndex);
    }

    /// <summary>
    /// Build the stock as a box enclosing the part, exactly like in the reference project
    /// </summary>
    private static void BuildBoxStock(ComWrapper<ICamApiTechOperation> rootOperationCom)
    {
        using var modelFormerCom = rootOperationCom.ModelFormerWorkpiece();
        using var withBoxCom = modelFormerCom.AsWithBoxPrimitives();
        using var boxItemCom = withBoxCom.AddBoxAroundPart();
        if (boxItemCom.IsNull)
            throw new Exception("Cannot build box stock around the part");

        // the reference project machines the part out of a billet without any extra material
        boxItemCom.Invoke(box =>
        {
            box.XMinStock = StockAllowance;
            box.XMaxStock = StockAllowance;
            box.YMinStock = StockAllowance;
            box.YMaxStock = StockAllowance;
            box.ZMinStock = StockAllowance;
            box.ZMaxStock = StockAllowance;
        });
    }

    /// <summary>
    /// Take a ready-made vise from the fixtures library, mount it on the same connector as the workpiece,
    /// place it under the part and clamp its jaw. The library package carries the whole vise — the body,
    /// the movable jaw, their travel limits and their geometry — so nothing is assembled by hand
    /// </summary>
    private static void AddVise(ComWrapper<ICamApiTechOperation> rootOperationCom, int connectorIndex)
    {
        using var modelFormerCom = rootOperationCom.ModelFormerFixtures();
        using var withFixturesCom = modelFormerCom.AsWithFixtures();
        using var viseCom = withFixturesCom.ImportComponentFromFile(
            FindLibraryComponent(withFixturesCom, ViseLibraryComponentName));
        if (viseCom.IsNull)
            throw new Exception($"Cannot import the '{ViseLibraryComponentName}' vise from the fixtures library");

        viseCom.SetConnectorIndex(connectorIndex);

        // an imported vise sits at the connector origin, so move it under the part
        using var componentCom = viseCom.GetComponent();
        componentCom.SetSetupLCS(ViseSetupLcs);

        // the vise is a chain of nodes: the jaw is a child of the body, not a second node of the component
        using var bodyCom = componentCom.GetNode(0);
        using var jawCom = bodyCom.GetNode(0);
        jawCom.SetPosition(JawPosition);
    }

    /// <summary>
    /// Find the file of a fixtures library package by its name
    /// </summary>
    private static string FindLibraryComponent(ComWrapper<ICamApiModelFormerWithFixtures> withFixturesCom,
        string componentName)
    {
        var available = new List<string>();
        for (var i = 0; i < withFixturesCom.LibraryComponentCount(); i++)
        {
            var componentFile = withFixturesCom.LibraryComponentFile(i);
            available.Add(componentFile);
            if (Path.GetFileNameWithoutExtension(componentFile) == componentName)
                return componentFile;
        }

        throw new Exception($"There is no '{componentName}' in the fixtures library."
                            + $" Available components: {string.Join(", ", available)}");
    }
}
