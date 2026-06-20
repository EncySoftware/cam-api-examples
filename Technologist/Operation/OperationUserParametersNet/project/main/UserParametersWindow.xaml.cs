using System.Text;
using System.Windows;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.TechOperation;

namespace OperationUserParametersNet;

/// <summary>
/// Window with Get / Add / Delete buttons that operate on the user parameters of the currently
/// selected technology operation. Owns the application COM wrapper handed to it and re-navigates
/// to the current operation on every click, so it always targets whatever is selected now.
/// </summary>
public partial class UserParametersWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiApplication> _applicationCom;

    /// <summary>Interaction logic for UserParametersWindow.xaml.</summary>
    public UserParametersWindow(ComWrapper<ICamApiApplication> applicationCom)
    {
        _applicationCom = applicationCom;
        InitializeComponent();
    }

    private void Get_Click(object sender, RoutedEventArgs e) => RunSafe(listCom =>
    {
        if (listCom == null)
        {
            OutputBox.Text = "The current operation has no user parameters " +
                             "(a group, the root operation, or an operation without a toolpath template).";
            return;
        }

        var count = listCom.Count();
        var report = new StringBuilder();
        report.AppendLine($"The current operation has {count} user parameter(s):");
        for (var i = 0; i < count; i++)
        {
            using var paramCom = listCom.GetItem(i);
            report.AppendLine($"[{i}] {paramCom.Name()} = \"{paramCom.Value()}\"  // {paramCom.Comment()}");
        }

        OutputBox.Text = report.ToString().TrimEnd();
    });

    private void Add_Click(object sender, RoutedEventArgs e) => RunSafe(listCom =>
    {
        if (listCom == null)
        {
            OutputBox.Text = "Cannot add a user parameter: the current operation has no user parameters template.";
            return;
        }

        using var paramCom = listCom.Add(
            OperationUserParameters.ExampleParamName,
            OperationUserParameters.ExampleParamValue,
            OperationUserParameters.ExampleParamComment);
        OutputBox.Text = $"Added/updated user parameter '{paramCom.Name()}' = \"{paramCom.Value()}\" " +
                         $"(comment: \"{paramCom.Comment()}\").";
    });

    private void Delete_Click(object sender, RoutedEventArgs e) => RunSafe(listCom =>
    {
        if (listCom == null)
        {
            OutputBox.Text = "The current operation has no user parameters to delete.";
            return;
        }

        // Demonstrate the lookup before deleting. The found wrapper must be disposed before
        // Delete invalidates the underlying COM object.
        string oldValue;
        using (var foundCom = listCom.FindByName(OperationUserParameters.ExampleParamName))
        {
            if (foundCom.IsNull)
            {
                OutputBox.Text = $"User parameter '{OperationUserParameters.ExampleParamName}' " +
                                 "was not found; nothing to delete.";
                return;
            }

            oldValue = foundCom.Value();
        }

        var deleted = listCom.Delete(OperationUserParameters.ExampleParamName);
        OutputBox.Text = $"Deleted user parameter '{OperationUserParameters.ExampleParamName}' " +
                         $"(was \"{oldValue}\"): {deleted}.";
    });

    private void RunSafe(Action<ComWrapper<ICamApiUserParametersList>?> action)
    {
        try
        {
            OperationUserParameters.WithUserParameters(_applicationCom, action);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>Dispose the application COM wrapper handed to this window.</summary>
    public void Dispose() => _applicationCom.Dispose();
}
