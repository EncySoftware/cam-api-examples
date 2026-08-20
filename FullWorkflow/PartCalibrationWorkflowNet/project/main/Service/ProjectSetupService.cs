using System;
using System.IO;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.ModelFormerTypes;
using CAMAPI.Project;
using CAMAPI.TechOperation;
using CAMAPI.Technologist;

namespace PartCalibrationWorkflowNet.Service;

/// <summary>
/// Tab 2 project-prep helpers. A probing operation is only creatable on a
/// machine that supports it and inside a technology Part/Setup; on an arbitrary
/// open project those may be missing. These two actions reproduce what a fresh
/// CreateNewProject used to provide: the default machine and the setup/part
/// structure.
/// </summary>
internal static class ProjectSetupService
{
    // The machine we treat as the calibration default: Maho DMU60, a 5-axis (BC)
    // milling machine — it supports the mill part-probing operation. Located
    // under the host's Machines folder (resolved via the Paths singleton) so no
    // absolute path is hard-coded.
    private const string DefaultMachineRelPath =
        @"Schemas\Milling\5-axis (BC)\Maho DMU60\DMU60.xml";
    private const string DefaultMachineTypeName = "DMU60";
    private static readonly Guid DefaultMachineGuid =
        Guid.Parse("F4BD0FFD-8C44-4CFC-A865-CF595EEEF38F");

    /// <summary>
    /// Set the active project's machine to the default calibration machine
    /// (LatheMillWithTurret). The machine file is found under the host Machines
    /// folder via the Paths singleton. Probing operations can only be created on
    /// a machine that supports them.
    /// </summary>
    public static string SetDefaultMachine(ComWrapper<ICamApiApplication> appCom)
    {
        using var pathsCom = SystemExtensionFactory.GetPathsHelper();
        var machinesFolder = pathsCom.Invoke(p => p.MachinesFolder)
            ?? throw new InvalidOperationException("Cannot resolve the machines folder.");
        var machineFile = Path.Combine(machinesFolder, DefaultMachineRelPath);

        using var libCom = appCom.MachinesLibrary();
        using var machineCom = libCom.FindMachine(DefaultMachineGuid, machineFile, DefaultMachineTypeName);
        if (machineCom.IsNull)
            throw new InvalidOperationException(
                $"Default machine '{DefaultMachineTypeName}' not found (looked at: {machineFile}).");
        var name = machineCom.MachineCaption();
        appCom.SetActiveProjectMachine(machineCom);
        return $"Project machine set to '{name}'.";
    }

    /// <summary>
    /// Ensure the technology tree has a setup stage with a workpiece casting.
    /// Idempotent: if a setup already exists, does nothing. Run after the machine
    /// is set — setup creation needs a machine that hosts it.
    /// </summary>
    public static string CreateSetups(ComWrapper<ICamApiProject> projCom)
    {
        using var techCom = projCom.Technologist();
        using var listCom = techCom.PartAndStageList();

        // The root operation is itself a setup stage, so the count is never 0.
        if (listCom.SetupStagesCount() > 1)
            return "Setup already exists.";

        // One stage: a real setup only if its operation GUID differs from root's.
        using var rootOpCom = techCom.RootOperation();
        using var stageCom = listCom.SetupStage(0);
        using var stageOpCom = stageCom.AsInstanceOf<ICamApiTechOperation>();
        if (stageOpCom is not null && stageOpCom.Id() != rootOpCom.Id())
            return "Setup already exists.";

        // A setup needs a Part — a part-less setup breaks the Setup inspector.
        using var partCom = listCom.PartsCount() == 0 ? techCom.CreatePart(1) : null;
        using var setupCom = techCom.CreateSetupStage();
        EnsureWorkpieceCasting(techCom);
        return "Created a setup with a workpiece casting.";
    }

    /// <summary>
    /// Give the current setup's workpiece a casting primitive with 1 mm stock.
    /// Best-effort: no-ops if the workpiece former has no casting support.
    /// </summary>
    private static void EnsureWorkpieceCasting(ComWrapper<ICamApiTechnologist> techCom)
    {
        using var opCom = techCom.CurrentOperation();
        if (opCom.IsNull)
            return;
        using var wpMfCom = opCom.ModelFormerWorkpiece();
        if (wpMfCom.IsNull)
            return;
        using var castingMfCom = wpMfCom.AsInstanceOf<ICamApiModelFormerWithCastingPrimitive>();
        if (castingMfCom is null)
            return;
        using var castingCom = castingMfCom.AddCasting();
        castingCom.SetStock(1.0);
    }
}
