using System.Reflection;
using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Project;

namespace WorkpieceMillingWorkflowNet;

/// <summary>
/// Operations over project settings
/// </summary>
public static class ProjectHelper
{
    /// <summary>
    /// Path to the machine schema shipped with the example, relative to the extension folder
    /// </summary>
    private const string MachineFilePath = @"resources\machine\Slovtos\Slovtos.xml";

    /// <summary>
    /// Machine identifier inside the schema file
    /// </summary>
    private static readonly Guid MachineGuid = Guid.Parse("F916895F-26E0-463E-B29C-995CF58C5C41");

    /// <summary>
    /// Machine type inside the schema file
    /// </summary>
    private const string MachineTypeName = "Slovtos";

    /// <summary>
    /// Path to the tool library shipped with the example, relative to the extension folder
    /// </summary>
    private const string ToolsLibraryPath = @"resources\tools\CornersCleanupTools.db";

    /// <summary>
    /// Identifier of the machine connector the workpiece and the fixtures are mounted on
    /// </summary>
    private const string BaseTableConnectorName = "BaseTableWrk";

    /// <summary>
    /// Path to the turn table selector inside the machine schema
    /// </summary>
    private const string TurnTableSelectorPath = "Schema.AxisY.AxisX.TurnTable.ActiveNode";

    /// <summary>
    /// Path to the tail stock selector inside the machine schema
    /// </summary>
    private const string TailStockSelectorPath = "Schema.AxisY.AxisX.TailStock.ActiveNode";

    /// <summary>
    /// Variant of a machine node selector which leaves the node out of the machine
    /// </summary>
    private const string EmptyNodeVariant = "Base0";

    /// <summary>
    /// Number of the roughing tool inside the tool library: end mill, diameter 14 mm
    /// </summary>
    public const string RoughingToolNumber = "1";

    /// <summary>
    /// Number of the scallop finishing tool inside the tool library: spherical mill, diameter 20 mm
    /// </summary>
    public const string ScallopToolNumber = "2";

    /// <summary>
    /// Number of the corners cleanup tool inside the tool library: spherical mill, diameter 10 mm
    /// </summary>
    public const string CornersToolNumber = "3";

    /// <summary>
    /// Prepare the project: change machine and load the cutting tools
    /// </summary>
    public static void PrepareProject(ComWrapper<ICamApiApplication> applicationCom,
        ComWrapper<ICamApiProject> activeProjectCom)
    {
        // switch to machining tab
        applicationCom.Invoke(application =>
        {
            application.MainWorkMode = TMainWorkMode.mwmMachining;
        });

        ChangeMachine(activeProjectCom);
        RemoveOptionalMachineNodes(activeProjectCom);
        SetupMachiningTools(applicationCom);
    }

    /// <summary>
    /// Leave the turn table and the tail stock out of the machine, like in the reference project
    /// </summary>
    private static void RemoveOptionalMachineNodes(ComWrapper<ICamApiProject> activeProjectCom)
    {
        using var machineCom = activeProjectCom.Machine();
        using var xmlPropCom = machineCom.XMLProp();
        xmlPropCom.SetStr(TurnTableSelectorPath, EmptyNodeVariant);
        xmlPropCom.SetStr(TailStockSelectorPath, EmptyNodeVariant);
    }

    /// <summary>
    /// Find the index of the base table connector among the workpiece connectors of the project machine
    /// </summary>
    public static int FindBaseTableConnectorIndex(ComWrapper<ICamApiProject> activeProjectCom)
    {
        using var machineCom = activeProjectCom.Machine();
        var connectorsCount = machineCom.WorkpieceConnectorsCount();

        // connectors are matched by their identifier, which differs from the caption shown in the UI
        var availableNames = new List<string>();
        for (var index = 0; index < connectorsCount; index++)
        {
            using var connectorCom = machineCom.WorkpieceConnector(index);
            var connectorName = connectorCom.Name();
            if (connectorName == BaseTableConnectorName)
                return index;

            availableNames.Add(connectorName);
        }

        throw new Exception($"Cannot find machine connector '{BaseTableConnectorName}', "
                            + $"available connectors: {string.Join(", ", availableNames)}");
    }

    /// <summary>
    /// Change machine of the project to the one shipped with the example
    /// </summary>
    private static void ChangeMachine(ComWrapper<ICamApiProject> activeProjectCom)
    {
        var machineFile = GetResourcePath(MachineFilePath);
        activeProjectCom.SetMachine(MachineGuid, machineFile, MachineTypeName);
    }

    /// <summary>
    /// Load the cutting tools of the whole technology from the tool library shipped with the example
    /// </summary>
    private static void SetupMachiningTools(ComWrapper<ICamApiApplication> applicationCom)
    {
        var toolsLibraryPath = GetResourcePath(ToolsLibraryPath);

        using var machiningToolManagerCom = applicationCom.InvokeAndWrap(application => application.MachiningToolsManager);
        machiningToolManagerCom.OpenExistingLibrary(toolsLibraryPath);
        machiningToolManagerCom.AddToolToProject(toolsLibraryPath, RoughingToolNumber);
        machiningToolManagerCom.AddToolToProject(toolsLibraryPath, ScallopToolNumber);
        machiningToolManagerCom.AddToolToProject(toolsLibraryPath, CornersToolNumber);
    }

    /// <summary>
    /// Build the full path to a resource file shipped next to the extension assembly
    /// </summary>
    public static string GetResourcePath(string relativePath)
    {
        var extensionFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
                              ?? throw new Exception("Cannot get extension folder");
        var resourceFile = Path.Combine(extensionFolder, relativePath);
        if (!File.Exists(resourceFile))
            throw new Exception($"Cannot find resource file: {resourceFile}");

        return resourceFile;
    }
}
