using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using PartCalibrationWorkflowNet.Model;
using PartCalibrationWorkflowNet.Service;
using STTypes;

namespace PartCalibrationWorkflowNet;

/// <summary>
/// Non-modal tabbed window for the full calibration workflow.
/// Owns <see cref="_appCom"/> and all services — disposes them when the window closes.
/// </summary>
public partial class CalibrationWorkflowWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiApplication> _appCom;
    private readonly string _pluginDir;

    private readonly PrepareProjectService _prepareService;
    private readonly MachineSimulationService _simulationService;
    private readonly SurfaceProjectionService _projectionService;
    private readonly CalibrationSolver _calibrationSolver;
    private readonly CalibrationApplyService _calibrationApplyService;

    /// <summary>NC file path produced by Generate NC; shared to Tab 3 display.</summary>
    private string? _ncPath;

    /// <summary>measured.json path produced by Simulate; displayed in Tab 3.</summary>
    private string? _measuredPath;

    public CalibrationWorkflowWindow(ComWrapper<ICamApiApplication> appCom)
    {
        _appCom = appCom;
        _pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        _prepareService = new PrepareProjectService(_pluginDir);
        _simulationService = new MachineSimulationService(_pluginDir);
        _projectionService = new SurfaceProjectionService();
        _calibrationSolver = new CalibrationSolver();
        _calibrationApplyService = new CalibrationApplyService();

        InitializeComponent();

        // Pre-fill paths if files already exist from a previous run
        var existingNc = Path.Combine(_pluginDir, "measurement.nc");
        if (File.Exists(existingNc))
            TxtNcPath.Text = _ncPath = existingNc;

        var existingMeasured = Path.Combine(_pluginDir, "measured.json");
        if (File.Exists(existingMeasured))
            TxtMeasuredPath.Text = _measuredPath = existingMeasured;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _projectionService.Dispose();
        _appCom.Dispose();
    }

    // ── Tab 1: Project ────────────────────────────────────────────────────────

    private async void BtnCreateProject_Click(object sender, RoutedEventArgs e)
    {
        await RunSafeAsync(TxtProjectStatus, () =>
        {
            _prepareService.CreateProject(_appCom);
            return "Project created. Click 'Generate NC'.";
        });
    }

    private async void BtnGenerateNc_Click(object sender, RoutedEventArgs e)
    {
        await RunSafeAsync(TxtNcStatus, () =>
        {
            _ncPath = _prepareService.GenerateNc(_appCom);
            return "NC generated and opened in Notepad.";
        });
        if (_ncPath != null)
        {
            TxtNcPath.Text = _ncPath;
            Process.Start(new ProcessStartInfo("notepad.exe", _ncPath) { UseShellExecute = true });
        }
    }

    // ── Tab 2: Simulation ─────────────────────────────────────────────────────

    private async void BtnSimulate_Click(object sender, RoutedEventArgs e)
    {
        if (!TryParseParams(out var p))
            return;
        await RunSafeAsync(TxtSimStatus, () =>
        {
            _measuredPath = _simulationService.Simulate(p);
            return "Simulation complete. measured.json written.";
        });
        if (_measuredPath != null)
            TxtMeasuredPath.Text = _measuredPath;
    }

    // ── Tab 3: Calibration ────────────────────────────────────────────────────

    private async void BtnCalibrate_Click(object sender, RoutedEventArgs e)
    {
        await RunSafeAsync(TxtCalibStatus, () =>
        {
            using var projCom = _appCom.GetActiveProject()
                ?? throw new Exception("No active project — click 'Create Project' first.");

            var measuredPoints = LoadMeasuredPoints();
            var nominalPoints = _projectionService.SnapToModel(projCom, measuredPoints);
            var matrix = _calibrationSolver.Solve(nominalPoints, measuredPoints);
            _calibrationApplyService.Apply(projCom, matrix);

            return "Calibration applied. Workpiece offset of Setup 2 updated.";
        });
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task RunSafeAsync(TextBox statusBlock, Func<string?> action)
    {
        SetBusy(true);
        try
        {
            var msg = await Task.Run(action);
            if (msg != null)
            {
                statusBlock.Foreground = Brushes.DarkGreen;
                statusBlock.Text = msg;
            }
        }
        catch (Exception ex)
        {
            statusBlock.Foreground = Brushes.Red;
            statusBlock.Text = $"Error: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        BtnCreateProject.IsEnabled = !busy;
        BtnGenerateNc.IsEnabled = !busy;
        BtnSimulate.IsEnabled = !busy;
        BtnCalibrate.IsEnabled = !busy;
        BusyIndicator.IsIndeterminate = busy;
        BusyIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private bool TryParseParams(out SimulatorParams p)
    {
        p = default!;
        if (!TryParseField(TxBox.Text, "TX", out double tx)) return false;
        if (!TryParseField(TyBox.Text, "TY", out double ty)) return false;
        if (!TryParseField(TzBox.Text, "TZ", out double tz)) return false;
        if (!TryParseField(RxBox.Text, "RX", out double rx)) return false;
        if (!TryParseField(RyBox.Text, "RY", out double ry)) return false;
        if (!TryParseField(RzBox.Text, "RZ", out double rz)) return false;
        p = new SimulatorParams(tx, ty, tz, rx, ry, rz);
        return true;
    }

    private static bool TryParseField(string text, string name, out double value)
    {
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;
        MessageBox.Show($"Invalid value for {name}: '{text}'", "Input Error",
            MessageBoxButton.OK, MessageBoxImage.Warning);
        return false;
    }

    private TST3DPoint[] LoadMeasuredPoints()
    {
        var path = _measuredPath ?? Path.Combine(_pluginDir, "measured.json");
        if (!File.Exists(path))
            throw new FileNotFoundException(
                "measured.json not found — run simulation first.", path);

        var data = JsonSerializer.Deserialize<MeasuredData>(File.ReadAllText(path))
            ?? throw new Exception("Failed to parse measured.json");

        return data.Points
            .Select(p => new TST3DPoint { X = p.X, Y = p.Y, Z = p.Z })
            .ToArray();
    }
}
