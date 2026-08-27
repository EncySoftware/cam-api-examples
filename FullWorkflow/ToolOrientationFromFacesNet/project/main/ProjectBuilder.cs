using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Machine;
using CAMAPI.Project;
using CAMAPI.Technologist;

using STTypes;

namespace ToolOrientationFromFacesNet;

/// <summary>
/// Builds the whole project the example works on: a new project with an imported part, a machine,
/// a workpiece setup and a row of operations
/// </summary>
public static class ProjectBuilder
{
    /// <summary>
    /// Shift of the workpiece coordinate system, which puts the origin into the part
    /// </summary>
    private static readonly TST3DPoint WorkpieceCoordinateSystemOffset = new() { X = -100, Y = -60, Z = 0 };

    /// <summary>
    /// Create a new project and fill it with everything the operations need
    /// </summary>
    public static void Build(ComWrapper<ICamApiApplication> applicationCom)
    {
        applicationCom.CreateNewProject();
        using var projectCom = applicationCom.GetActiveProject();
        using var technologistCom = projectCom.Technologist();

        ImportPart(applicationCom, projectCom);
        SetMachine(applicationCom);
        MountWorkpiece(projectCom, technologistCom);
        CreateOperations(technologistCom);
    }

    /// <summary>
    /// Import the part on the model tab
    /// </summary>
    private static void ImportPart(ComWrapper<ICamApiApplication> applicationCom, ComWrapper<ICamApiProject> projectCom)
    {
        var importFile = ExampleSettings.ImportFilePath;
        if (!File.Exists(importFile))
            throw new Exception($"Cannot find file to import: {importFile}");

        applicationCom.SetMainWorkMode(TMainWorkMode.mwmModel);

        using var importerCom = projectCom.GeomImporter();
        importerCom.ImportFile(importFile, "Part", false);
    }

    /// <summary>
    /// Put the robot into the project, taken from the machines library of the CAM system
    /// </summary>
    /// <remarks>
    /// The file path of FindMachine is optional: given a guid it resolves the machine out of the
    /// installed library, models and all. Passing a path is only needed for a schema that lives
    /// outside the machines folder of the installation.
    /// </remarks>
    private static void SetMachine(ComWrapper<ICamApiApplication> applicationCom)
    {
        applicationCom.SetMainWorkMode(TMainWorkMode.mwmMachining);

        using var machinesLibraryCom = applicationCom.MachinesLibrary();
        using var machineInfoCom = machinesLibraryCom.FindMachine(
            ExampleSettings.MachineGuid, "", ExampleSettings.MachineTypeName);
        if (machineInfoCom.IsNull)
            throw new Exception($"The machines library has no machine {ExampleSettings.MachineTypeName}");

        applicationCom.SetActiveProjectMachine(machineInfoCom);
    }

    /// <summary>
    /// Mount the workpiece on the positioner of the machine. An operation without a setup gets no
    /// toolpath at all, and then there is nothing for the orientation to act on
    /// </summary>
    /// <remarks>
    /// Which connector the part sits on decides whether the external axes take part in the solution:
    /// mounted on the floor the positioner is invisible to the solver, RobotTableAxesCount reads 0
    /// and no Rotate flip is offered.
    /// </remarks>
    private static void MountWorkpiece(ComWrapper<ICamApiProject> projectCom,
        ComWrapper<ICamApiTechnologist> technologistCom)
    {
        using var machineCom = projectCom.Machine();

        using var partAndStageListCom = technologistCom.PartAndStageList();
        using var partStageCom = partAndStageListCom.GetPartStage(0, 0);

        using (var workpieceSetupCom = partStageCom.WorkpieceSetup())
            workpieceSetupCom.SetMachineSideConnectorIndex(FindWorkpieceConnector(machineCom));

        using (var workpieceCoordinateSystemCom = partStageCom.WorkpieceCoordinateSystem())
            workpieceCoordinateSystemCom.SetOffset(WorkpieceCoordinateSystemOffset);
    }

    /// <summary>
    /// Index of the connector the part is mounted on, looked up by name
    /// </summary>
    /// <remarks>
    /// Index 0 is not the table: on this robot it is the floor, which no axis moves.
    /// </remarks>
    private static int FindWorkpieceConnector(ComWrapper<ICamApiMachine> machineCom)
    {
        var names = new List<string>();
        for (var i = 0; i < machineCom.WorkpieceConnectorsCount(); i++)
        {
            using var connectorCom = machineCom.WorkpieceConnector(i);
            var name = connectorCom.Name();
            if (name == ExampleSettings.WorkpieceConnectorName)
                return i;

            names.Add(name);
        }
        throw new Exception($"The machine has no connector named '{ExampleSettings.WorkpieceConnectorName}', "
                            + $"it offers: {string.Join(", ", names)}");
    }

    /// <summary>
    /// Create the row of identical operations, each of which gets its own orientation later. The
    /// operations keep the default tool of the machine, so the example needs no tool library
    /// </summary>
    private static void CreateOperations(ComWrapper<ICamApiTechnologist> technologistCom)
    {
        using var rootOperationCom = technologistCom.RootOperation();
        var previousId = rootOperationCom.Id();

        for (var i = 0; i < ExampleSettings.OperationsCount; i++)
        {
            using var operationCom = technologistCom.CreateOperation(ExampleSettings.OperationTypeId, previousId, "");
            previousId = operationCom.Id();
        }
    }
}
