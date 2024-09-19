using System;
using System.ComponentModel;
using System.Windows;
using CAMAPI.ResultStatus;
using CAMIPC.ExecuteContext;
using Microsoft.Win32;

namespace ApplicationEmptyNet;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
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
            var application = CamHelper.GetApplication();
            application.OpenProject(openFileDialog.FileName, true, ref executeContext);
            if (executeContext.ResultStatus.Code != TResultStatusCode.rsSuccess)
                throw new Exception(executeContext.ResultStatus.Description); 
        } 
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_OnClosing(object? sender, CancelEventArgs e)
    {
        CamHelper.Clean();
    }
}