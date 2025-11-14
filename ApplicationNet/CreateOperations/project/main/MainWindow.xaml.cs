using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using CAMAPI.DotnetHelper;
using CAMAPI.ResultStatus;
using CAMIPC.ExecuteContext;

namespace ApplicationCreateOperations;

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

        // fill the list with available operations to be added
        try
        {
            // get CAM application
            var executeContext = new TExecuteContext();
            var applicationCom = CamHelper.GetApplication();
            
            // get the current project
            using var activeProjectCom = applicationCom.InvokeAndWrap(app => app.GetActiveProject(ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            // technologist
            using var technologistCom = activeProjectCom.InvokeAndWrap(project => project.GetTechnologist(ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            // get the list of operations types
            var ids = new List<string>();
            using var operationTypesCom = technologistCom.InvokeAndWrap(technologist => technologist.OperationTypes);
            operationTypesCom.Invoke(operationTypes =>
            {
                do
                {
                    using var operationTypeCom = ComWrapper.Create(operationTypes.Current());
                    ids.Add(operationTypeCom.Invoke(operationType => $"{operationType.Id}"));
                } while (operationTypes.Next());
            });
            
            // fill the list with available operations
            OperationsTypesListBox.Items.Clear();
            foreach (var id in ids)
                OperationsTypesListBox.Items.Add(id);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private void CreateOperationButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // get selected item
            if (OperationsTypesListBox.SelectedItem == null)
            {
                MessageBox.Show("Please select an operation type.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var operationTypeId = OperationsTypesListBox.SelectedItem.ToString();
            
            // get CAM application
            var executeContext = new TExecuteContext();
            var applicationCom = CamHelper.GetApplication();
            
            // get the current project
            using var activeProjectCom = applicationCom.InvokeAndWrap(app => app.GetActiveProject(ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            // technologist
            using var technologistCom = activeProjectCom.InvokeAndWrap(project => project.GetTechnologist(ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
            
            // create new operation after current operation
            using var operationCom = technologistCom.InvokeAndWrap(technologist =>
                technologist.CreateOperation(operationTypeId, "", "", ref executeContext));
            if (executeContext.ResultStatus.Code == TResultStatusCode.rsError)
                throw new Exception(executeContext.ResultStatus.Description);
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