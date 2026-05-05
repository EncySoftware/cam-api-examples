using System.Globalization;
using System.Windows;
using PartCalibrationWorkflowNet.Model;

namespace PartCalibrationWorkflowNet;

/// <summary>
/// Modal WPF dialog for entering 6-DOF machine offset parameters.
/// Must be shown on an STA thread.
/// </summary>
public partial class SimulatorParamsWindow : Window
{
    internal SimulatorParams Params { get; private set; } =
        new SimulatorParams(2, 1, 0.5, 0.3, -0.2, 1.0);

    /// <summary>Initializes the window with default 6-DOF values.</summary>
    public SimulatorParamsWindow()
    {
        InitializeComponent();
    }

    private void OkClicked(object sender, RoutedEventArgs e)
    {
        if (!TryParseAll(out var p)) return;
        Params = p;
        DialogResult = true;
    }

    private void CancelClicked(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private bool TryParseAll(out SimulatorParams p)
    {
        p = default!;
        if (!TryParse(TxBox.Text, "TX", out double tx)) return false;
        if (!TryParse(TyBox.Text, "TY", out double ty)) return false;
        if (!TryParse(TzBox.Text, "TZ", out double tz)) return false;
        if (!TryParse(RxBox.Text, "RX", out double rx)) return false;
        if (!TryParse(RyBox.Text, "RY", out double ry)) return false;
        if (!TryParse(RzBox.Text, "RZ", out double rz)) return false;
        p = new SimulatorParams(tx, ty, tz, rx, ry, rz);
        return true;
    }

    private bool TryParse(string text, string name, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;
        MessageBox.Show($"Invalid value for {name}: '{text}'", "Input error",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }
}
