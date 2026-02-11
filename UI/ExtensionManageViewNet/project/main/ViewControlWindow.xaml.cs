using System.Windows;
using System.Windows.Controls;
using CAMAPI.DotnetHelper;
using CAMAPI.ViewCube;
using CAMAPI.ViewPort;

namespace ExtensionManageViewNet;

/// <summary>
/// Interaction logic for ViewControlWindow.xaml
/// </summary>
public partial class ViewControlWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiViewPort> _viewPortCom;

    /// <summary>
    /// Interaction logic for ViewControlWindow.xaml
    /// </summary>
    public ViewControlWindow(ComWrapper<ICamApiViewPort> viewPortCom)
    {
        _viewPortCom = viewPortCom;
        InitializeComponent();
    }

    private void Face_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string tagStr)
        {
            if (Enum.TryParse<TViewCubeRotateMode>(tagStr, out var mode))
            {
                try
                {
                    _viewPortCom.ZoomAll(true);
                    using var cubeCom = _viewPortCom.GetCube();
                    cubeCom.Rotate(mode);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }

    /// <summary>
    /// Dispose COM wrappers
    /// </summary>
    public void Dispose()
    {
        _viewPortCom.Dispose();
    }
}
