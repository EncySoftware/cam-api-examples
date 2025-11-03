using System.Diagnostics.CodeAnalysis;
using CAMAPI.Application;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;
using CAMAPI.Singletons;

namespace FullWorkflow3DProject;

/// <summary>
/// Import Part1.igs, create project and export G-code
/// </summary>
[SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
public class ExtensionFullWorkflow3DProject : IExtension, IExtensionUtility
{
    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <summary>
    /// Import Part1.igs, create project and export G-code
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
        using var geometryModelCom = activeProjectCom.InvokeAndWrap(project => project.CAMAPIGeomModel);
        using var geometryTreeIteratorCom =
            geometryModelCom.InvokeAndWrap(model => (model.GetNodes(out var status), status));
        using var technologistCom = activeProjectCom.InvokeAndWrap(project => project.Technologist);
            
        try
        {
            // freeze UI
            applicationMainFormCom.Invoke(applicationMainForm =>
                applicationMainForm.BeginFreeze((ushort)TFreezeInterfaceType.afiiGeneral));
                
            // prepare model
            ModelHelper.PrepareModel(applicationCom, activeProjectCom, pathsHelperCom);
            
            // prepare the project
            ProjectHelper.PrepareProject(applicationCom, pathsHelperCom);

            // create technology
            TechnologyHelper.CreateTechnology(applicationCom,
                activeProjectCom,
                technologistCom,
                geometryModelCom,
                pathsHelperCom);
            
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