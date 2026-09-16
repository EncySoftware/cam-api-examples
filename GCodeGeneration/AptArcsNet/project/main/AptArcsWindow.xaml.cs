using System.IO;
using System.Windows;
using AptArcsNet.Model;
using AptArcsNet.Service;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;

namespace AptArcsNet;

/// <summary>
/// Non-modal window of the example. The button click marshals the whole CAMAPI chain
/// (create operation → load APT → calculate → read McdTree) onto an MTA thread via
/// <see cref="MtaTaskScheduler"/>, because the host is MTA while this window runs on STA,
/// and brings back only plain data for the list.
/// </summary>
public partial class AptArcsWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiApplication> _appCom;
    private readonly AptArcsService _service;

    private AptArcsReport? _report;

    /// <param name="appCom">Live ENCY application; the window takes ownership and disposes it</param>
    public AptArcsWindow(ComWrapper<ICamApiApplication> appCom)
    {
        _appCom = appCom;
        _service = new AptArcsService(appCom);
        InitializeComponent();

        // Apply the host ENCY light/dark theme once the dispatcher is up.
        Loaded += (_, _) => ThemeService.Apply(this, _appCom);
    }

    /// <inheritdoc />
    public void Dispose() => _appCom.Dispose();

    private async void BtnRun_Click(object sender, RoutedEventArgs e)
    {
        SetBusy(true);
        StatusText.Text = "Calculating…";
        try
        {
            _report = await MtaTaskScheduler.Run(() => _service.Run());
            FillList();

            SummaryText.Text =
                $"{_report.ArcCount} arcs ({_report.RadiiSummary()}), {_report.LineCount} straight moves, " +
                $"{_report.Nodes.Count} nodes total — ENCY reports {_report.ReportedArcs} arcs " +
                $"and {_report.ReportedLines} straight blocks for the operation";

            StatusText.Text = _report.ArcCount > 0
                ? "The CIRCLE records of the program are arcs in the toolpath — radius and centre kept."
                : "No arcs in the toolpath. If the calculation ran without an interpreter, pick "
                  + Path.GetFileName(await MtaTaskScheduler.Run(() => _service.InterpreterPath()))
                  + " in the operation's Job assignment tab and run again.";
        }
        catch (Exception ex)
        {
            StatusText.Text = $"Error: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ArcsOnly_Changed(object sender, RoutedEventArgs e) => FillList();

    private void FillList()
    {
        NodesList.Items.Clear();
        if (_report is null)
            return;

        var arcsOnly = ChkArcsOnly.IsChecked == true;
        foreach (var node in _report.Nodes)
        {
            if (arcsOnly && !node.IsArc)
                continue;
            NodesList.Items.Add(node);
        }
    }

    private void SetBusy(bool busy)
    {
        BtnRun.IsEnabled = !busy;
        BusyIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }
}
