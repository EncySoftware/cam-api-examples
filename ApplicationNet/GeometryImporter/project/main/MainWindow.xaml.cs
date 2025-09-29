using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMIPC.ExecuteContext;
using Microsoft.Win32;

namespace GeometryImporter;

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

    private void OpenFileButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // ask the user to select a file
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select a model file to be imported",
                Filter = "IGS|*.igs"
            };
            if (openFileDialog.ShowDialog() != true)
                return;
            
            // open in CAM application
            var executeContext = new TExecuteContext();
            var applicationCom = CamHelper.GetApplication();
            
            // get the current project
            using var activeProjectCom = applicationCom.InvokeAndWrap(app => app.GetActiveProject(ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            // importer
            using var importerCom = activeProjectCom.InvokeAndWrap(activeProject => activeProject.GetGeomImporter(ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            // import file
            var modelFileName = openFileDialog.FileName;
            importerCom.Invoke(importer =>
            {
                importer.ImportFile(modelFileName, "", true, ref executeContext);
                if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
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