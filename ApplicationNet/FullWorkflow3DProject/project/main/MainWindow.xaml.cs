using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using CAMAPI.ApplicationMainForm;
using CAMAPI.DotnetHelper;
using CAMIPC.ExecuteContext;

namespace FullWorkflow3DProject;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    private CamHelper CamHelper { get; }
    
    public MainWindow()
    {
        InitializeComponent();
        ComWrapperSettings.ApplicationApartmentState = ApartmentState.STA;
        CamHelper = new CamHelper();
    }

    private void Run_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // get CAM application
            var executeContext = new TExecuteContext();
            var applicationCom = CamHelper.GetApplication();
            
            // arrange COM wrappers
            using var pathsHelperCom = applicationCom.InvokeAndWrap(application => application.Paths);
            using var applicationMainFormCom = applicationCom.InvokeAndWrap(application => application.MainForm);
            
            using var activeProjectCom =
                applicationCom.InvokeAndWrap(application => application.GetActiveProject(ref executeContext));
            if (executeContext.ResultStatus.Code == CAMAPI.ResultStatus.TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            if (activeProjectCom == null)
                throw new Exception("Active project is not found");
            
            using var geometryModelCom = activeProjectCom.InvokeAndWrap(project => project.GeomModel);
            using var geometryTreeIteratorCom =
                geometryModelCom.InvokeAndWrap(model => model.GetNodes(ref executeContext));
            if (executeContext.ResultStatus.Code == CAMAPI.ResultStatus.TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            using var technologistCom = activeProjectCom.InvokeAndWrap(project => project.GetTechnologist(ref executeContext));
            if (executeContext.ResultStatus.Code == CAMAPI.ResultStatus.TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            try
            {
                // freeze UI
                applicationMainFormCom.Invoke(applicationMainForm => applicationMainForm.BeginFreeze(TFreezeInterfaceType.afiiGeneral));
                
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
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        CamHelper.Dispose();
    }
}