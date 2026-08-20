using System.Diagnostics.CodeAnalysis;
using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;

namespace WorkpieceMillingWorkflowNet;

/// <summary>
/// Import a part, build the stock as a box through the workpiece model former, add a vise fixture,
/// reproduce the technology of the "Corners cleanup" sample project, simulate it and generate G-code
/// </summary>
[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
public class ExtensionWorkpieceMillingWorkflowNet : IExtension, IExtensionUtility
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

    private void RunInternal(IExtensionUtilityContext context)
    {
        // arrange COM wrappers
        using var pathsHelperCom = SystemExtensionFactory.GetSingletonExtension<ICamApiPaths>("Extension.Global.Singletons.Paths");
        using var applicationCom = ComWrapper.Create(context.CamApplication);
        using var applicationMainFormCom = applicationCom.InvokeAndWrap(application => application.MainForm);
        using var activeProjectCom = applicationCom.InvokeAndWrap(application =>
            (application.GetActiveProject(out var status), status));
        if (activeProjectCom == null)
            throw new Exception("Active project is not found");
        using var technologistCom = activeProjectCom.InvokeAndWrap(project => project.Technologist);

        try
        {
            // freeze UI
            applicationMainFormCom.Invoke(applicationMainForm =>
                applicationMainForm.BeginFreeze((ushort)TFreezeInterfaceType.afiiGeneral));

            // set the machine, strip its optional nodes and load the cutting tools shipped with the example
            ProjectHelper.PrepareProject(applicationCom, activeProjectCom);

            // the connector list depends on the machine nodes, so look up the index only now
            var baseTableConnectorIndex = ProjectHelper.FindBaseTableConnectorIndex(activeProjectCom);

            // mount the workpiece on the base table of the machine
            WorkpieceHelper.MountWorkpiece(technologistCom, baseTableConnectorIndex);

            // import a part to machine
            ModelHelper.PrepareModel(applicationCom, activeProjectCom, pathsHelperCom);

            // build the stock as a box and take a vise from the fixtures library
            WorkpieceHelper.SetupStockAndFixtures(technologistCom, baseTableConnectorIndex);

            // create the roughing, scallop and corners cleanup operations and calculate the toolpath
            TechnologyHelper.CreateTechnology(applicationCom, activeProjectCom, technologistCom, pathsHelperCom);

            // simulate and save results
            SimulationHelper.RunSimulation(applicationCom, activeProjectCom);
        }
        finally
        {
            // unfreeze UI
            applicationMainFormCom.Invoke(applicationMainForm => applicationMainForm.EndFreeze());
        }
    }
}
