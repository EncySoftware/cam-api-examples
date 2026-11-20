using System.Windows;
using System.Windows.Controls;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;

namespace ExtensionMainTabNet;

/// <summary>
/// Content of the tab. It runs on the thread of the page: it calls ENCY only through ComWrapper and gets the values
/// read on the ENCY main thread from <see cref="ExtensionMainTab"/>
/// </summary>
public partial class MainTabPage : UserControl
{
    private readonly ComWrapper<ICamApiApplication> _applicationCom;

    /// <summary>
    /// Page calling ENCY through the wrapper; the wrapper is released by <see cref="ExtensionMainTab"/>
    /// </summary>
    public MainTabPage(ComWrapper<ICamApiApplication> applicationCom, ThemePalette palette)
    {
        InitializeComponent();
        palette.ApplyTo(Resources);
        _applicationCom = applicationCom;
    }

    /// <summary>
    /// Shows the name of the active project in the colours of the current theme
    /// </summary>
    public void Show(string projectName, ThemePalette palette)
    {
        palette.ApplyTo(Resources);
        ProjectText.Text = projectName;
    }

    private void OnModelClick(object sender, RoutedEventArgs e)
    {
        ErrorText.Visibility = Visibility.Collapsed;
        try
        {
            // ComWrapper runs the call on an MTA thread and processes the messages of the page while it waits
            using var mainFormCom = _applicationCom.MainForm();
            mainFormCom.SetActiveMainTab(MainTabIds.Model);
        }
        catch (Exception exception)
        {
            ErrorText.Text = exception.Message;
            ErrorText.Visibility = Visibility.Visible;
        }
    }
}
