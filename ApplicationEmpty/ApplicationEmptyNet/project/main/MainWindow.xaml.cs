using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMIPC.ExecuteContext;
using Microsoft.Win32;

namespace ApplicationEmptyNet;

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

    private void OpenProjectButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // ask the user to select a file
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a project file",
                Filter = "Project (*.stcp)|*.stcp"
            };
            if (openFileDialog.ShowDialog() != true)
                return;

            // open in CAM application
            var executeContext = new TExecuteContext();
            var applicationCom = CamHelper.GetApplication();
            applicationCom.Invoke(app =>
            {
                if (app == null)
                    throw new Exception("Can't get application instance");
                app.OpenProject(openFileDialog.FileName, true, ref executeContext);
                if (executeContext.ResultStatus.Code != TResultStatusCode.rsSuccess)
                    throw new Exception(executeContext.ResultStatus.Description);
            });
            
            // get current project
            using var activeProjectCom = applicationCom.Invoke(app =>
            {
                if (app == null)
                    throw new Exception("Can't get application instance");
                return ComWrapper.Create(app.GetActiveProject(ref executeContext));
            });
            
            // get technologist
            using var technologistCom = activeProjectCom.Invoke(project =>
            {
                if (project == null)
                    throw new Exception("Can't get project instance");
                return ComWrapper.Create(project.GetTechnologist(ref executeContext));
            });
            
            // calculate toolpath
            technologistCom.Invoke(technologist =>
            {
                if (technologist == null)
                    throw new Exception("Can't get technologist instance");
                technologist.CalculateToolpath(true, ref executeContext);
                if (executeContext.ResultStatus.Code != TResultStatusCode.rsSuccess)
                    throw new Exception(executeContext.ResultStatus.Description);
            });
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