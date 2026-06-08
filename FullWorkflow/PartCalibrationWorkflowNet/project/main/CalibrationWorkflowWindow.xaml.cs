using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using Microsoft.Win32;
using PartCalibrationWorkflowNet.Model;
using PartCalibrationWorkflowNet.Service;
using STTypes;

namespace PartCalibrationWorkflowNet;

/// <summary>
/// Non-modal 5-tab calibration wizard (per ComplexCalibration.md).
/// Owns the COM wrapper for the CAM application and all wizard services.
/// </summary>
public partial class CalibrationWorkflowWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiApplication> _appCom;
    private readonly string _pluginDir;

    private readonly SettingsRepository _settingsRepo;
    private WizardSettings _settings;

    private readonly SurfaceProjectionService _projection = new();
    private readonly DeviationCalculationService _deviation = new();



    /// <summary>Last calculation result, shared between Tab 4 and Tab 5.</summary>
    private CalibrationResult? _lastResult;
    private string _lastFormattedResult = "";

    /// <summary>Polls the viewport selection so the Tab 1 counter stays live.</summary>
    private DispatcherTimer? _selectionTimer;
    private bool _selectionCountBusy;

    public CalibrationWorkflowWindow(ComWrapper<ICamApiApplication> appCom)
    {
        DbgLog.Write("Window ctor enter");
        _appCom = appCom;
        _pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

        _settingsRepo = new SettingsRepository(_pluginDir);
        _settings = _settingsRepo.Load();

        InitializeComponent();
        DbgLog.Write("InitializeComponent done");

        // Populate UI from settings only — no COM calls during construction
        InitializeStaticControls();
        DbgLog.Write("Static controls initialized");

        // Project-derived combos and theme application both touch _appCom;
        // postpone them until the window's dispatcher is fully spun up.
        Loaded += OnWindowLoaded;
        Closed += (_, _) => Dispose();
        // Refresh the selection counters whenever the user returns to this
        // window (i.e. after selecting surfaces/points in the host).
        Activated += (_, _) => UpdateSelectionCounters();
        DbgLog.Write("Window ctor exit");
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        DbgLog.Write("OnWindowLoaded enter");

        // Theme palette from ICamApiTheme (commit 9c13e114, SDK 2.0.18-dev.34+).
        // Silently no-ops on older hosts that do not implement the interface.
        try { ThemeService.Apply(this, _appCom); }
        catch (Exception ex) { DbgLog.Write("Theme apply failed", ex); }

        try { RefreshOperationsCombo();  }
        catch (Exception ex) { DbgLog.Write("RefreshOperationsCombo failed", ex); }

        try { RefreshPartStagesCombo(); }
        catch (Exception ex) { DbgLog.Write("RefreshPartStagesCombo failed", ex); }

        try { RefreshGroupCombos(); }
        catch (Exception ex) { DbgLog.Write("RefreshGroupCombos failed", ex); }

        UpdateSelectionCounters();

        // Poll the host selection so the Tab 1/2 counters update live, even while
        // this window does not have focus (the user selects in the host).
        _selectionTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(750) };
        _selectionTimer.Tick += (_, _) => UpdateSelectionCounters();
        _selectionTimer.Start();

        // Output format default comes from WizardSettings (persisted) — no
        // need to probe Machine.XMLProp. The combobox covers every format
        // anyway and the user's last choice survives across runs.
        DbgLog.Write("OnWindowLoaded exit");

        // Smoke-test hook: when env var PARTCALIB_AUTOCLICK is set we
        // synthesise clicks through the wizard so a headless scenario can
        // reproduce the same code paths the user takes.
        //   = "CreatePoints"  → Tab 1 only.
        //   = "All"           → full Tab 1→5 cycle with selection management
        //                       between steps (creates a synthetic measured
        //                       points file for Tab 3).
        var autoMode = Environment.GetEnvironmentVariable("PARTCALIB_AUTOCLICK");
        if (autoMode == "CreatePoints")
        {
            DbgLog.Write("Auto-clicking CreatePoints (PARTCALIB_AUTOCLICK)");
            Dispatcher.BeginInvoke(new Action(() =>
                BtnCreatePoints_Click(this, new RoutedEventArgs())));
        }
        else if (autoMode == "All")
        {
            DbgLog.Write("Auto-running full wizard (PARTCALIB_AUTOCLICK=All)");
            _ = RunFullSelfTestAsync();
        }
    }

    /// <summary>
    /// Walks the wizard end-to-end programmatically: Tab 1 → 2 → 3 → 4 → 5.
    /// Used only by the headless smoke scenario; gated by env var.
    /// </summary>
    private async Task RunFullSelfTestAsync()
    {
        try
        {
            // Tab 1: create the point cloud over selected faces.
            DbgLog.Write("SelfTest: Tab 1 click");
            await ClickAndWaitAsync(BtnCreatePoints, _ => BtnCreatePoints_Click(this, _));

            // Tab 2: re-select the freshly created points so probing cycles
            //        find something to attach to. Auto-create the probing
            //        operation by leaving operationId null (combobox default).
            DbgLog.Write("SelfTest: selecting newly created points for Tab 2");
            SelectTreeNodesByEntityType(CAMAPI.GeomModel.TCAMAPIGeometryEntityType.etPoint);
            DbgLog.Write("SelfTest: Tab 2 click");
            await ClickAndWaitAsync(BtnCreateCycles, _ => BtnCreateCycles_Click(this, _));

            // Tab 3: synthesize a Plain-text measured file with the points
            //        from Tab 1 shifted by a small known offset, then click.
            DbgLog.Write("SelfTest: synthesising measured.txt");
            var measuredPath = Path.Combine(Path.GetTempPath(),
                $"PartCalib_selftest_measured_{Environment.ProcessId}.txt");
            WriteSyntheticMeasuredFile(measuredPath);
            TxtMeasuredFilePath.Text  = measuredPath;
            TxtMeasuredFolderName.Text = "MeasuredPoints";
            CmbMeasuredParser.SelectedItem = "Plain text (X;Y;Z)";
            DbgLog.Write("SelfTest: Tab 3 click");
            await ClickAndWaitAsync(BtnImportMeasured, _ => BtnImportMeasured_Click(this, _));

            // Tab 4: deselect points, reselect faces, point the deviation
            //        folder at the imported "MeasuredPoints" group.
            DbgLog.Write("SelfTest: selecting faces for Tab 4");
            SelectTreeNodesByEntityType(CAMAPI.GeomModel.TCAMAPIGeometryEntityType.etFace);
            // Tab 3 just created the MeasuredPoints group — refresh the combos so
            // it appears, then select it.
            RefreshGroupCombos();
            foreach (string item in CmbDeviationPointsFolder.Items)
                if (item.IndexOf("MeasuredPoints", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    CmbDeviationPointsFolder.SelectedItem = item;
                    break;
                }
            DbgLog.Write("SelfTest: Tab 4 click");
            await ClickAndWaitAsync(BtnCalculateDeviation,
                _ => BtnCalculateDeviation_Click(this, _));

            // Tab 5: simplest apply mode — create LCS.
            DbgLog.Write("SelfTest: Tab 5 click (CreateLCS)");
            SelectComboItemByTag(CmbApplyMode, "CreateLCS");
            TxtLcsName.Text = "CalibratedLCS_SelfTest";
            await ClickAndWaitAsync(BtnApply, _ => BtnApply_Click(this, _));

            DbgLog.Write("SelfTest: completed all 5 tabs");
        }
        catch (Exception ex)
        {
            DbgLog.Write("SelfTest aborted", ex);
        }
    }

    private async Task ClickAndWaitAsync(System.Windows.Controls.Button button, Action<RoutedEventArgs> click)
    {
        click(new RoutedEventArgs());
        // Each click handler is `async void` driven by RunSafeAsync; the
        // BusyIndicator drops back to Collapsed when the action finishes.
        // Give it a generous 60 s — geometry imports can be slow on a cold
        // project.
        for (int i = 0; i < 600 && BusyIndicator.Visibility == Visibility.Visible; i++)
            await Task.Delay(100);
        if (BusyIndicator.Visibility == Visibility.Visible)
            DbgLog.Write($"ClickAndWaitAsync({button.Name}): timed out after 60 s");
        await Task.Delay(200);
    }

    private void SelectTreeNodesByEntityType(CAMAPI.GeomModel.TCAMAPIGeometryEntityType type)
    {
        using var projCom = _appCom.GetActiveProject();
        if (projCom is null) return;
        using var geomCom = projCom.CAMAPIGeomModel();
        geomCom.Invoke(model => model.DeselectAll());
        foreach (var nodeCom in geomCom.EnumerateNodes())
        {
            using var entityCom = nodeCom.GeometryEntity();
            if (entityCom.IsNull) continue;
            if (entityCom.EntityType() != type) continue;
            nodeCom.Invoke(n => n.Selected = true);
        }
    }

    private void WriteSyntheticMeasuredFile(string path)
    {
        var n = WriteEmulatedMeasuredFile(_appCom, path, 2.0, 1.0, 0.5, 0.3, -0.2, 1.0);
        DbgLog.Write($"SelfTest: wrote {n} measured points to {path}");
    }

    /// <summary>
    /// Emulate a machine measurement report from the points CURRENTLY SELECTED on
    /// the Model page (the same points the probing cycles were made for): bake
    /// them into a Plain-text (X;Y;Z) file with a small known offset. Returns the
    /// number of points written.
    /// </summary>
    private static int WriteEmulatedMeasuredFile(
        ComWrapper<ICamApiApplication> appCom, string path,
        double tx, double ty, double tz, double rxDeg, double ryDeg, double rzDeg)
    {
        using var projCom = appCom.GetActiveProject()
            ?? throw new InvalidOperationException("No active project — open a project first.");
        var selected = Service.ProbingCyclesService.ReadSelectedNamedPoints(projCom);
        if (selected.Count == 0)
            throw new InvalidOperationException(
                "No points selected — select the probing points on the Model page first.");

        // 6-DOF rigid transform: rotate (R = Rz*Ry*Rx, degrees) then translate.
        double rx = rxDeg * Math.PI / 180.0, ry = ryDeg * Math.PI / 180.0, rz = rzDeg * Math.PI / 180.0;
        double cx = Math.Cos(rx), sx = Math.Sin(rx);
        double cy = Math.Cos(ry), sy = Math.Sin(ry);
        double cz = Math.Cos(rz), sz = Math.Sin(rz);
        double r00 = cz * cy, r01 = cz * sy * sx - sz * cx, r02 = cz * sy * cx + sz * sx;
        double r10 = sz * cy, r11 = sz * sy * sx + cz * cx, r12 = sz * sy * cx - cz * sx;
        double r20 = -sy,     r21 = cy * sx,                r22 = cy * cx;

        // Emit "name;x;y;z" so re-imported points keep the original names.
        var lines = selected.Select(p =>
            $"{p.Name};" +
            $"{r00 * p.X + r01 * p.Y + r02 * p.Z + tx:F6};" +
            $"{r10 * p.X + r11 * p.Y + r12 * p.Z + ty:F6};" +
            $"{r20 * p.X + r21 * p.Y + r22 * p.Z + tz:F6}");
        File.WriteAllLines(path, lines);
        return selected.Count;
    }

    private static void SelectComboItemByTag(System.Windows.Controls.ComboBox combo, string tag)
    {
        foreach (var item in combo.Items)
        {
            if (item is System.Windows.Controls.ComboBoxItem ci &&
                string.Equals(ci.Tag as string, tag, StringComparison.Ordinal))
            {
                combo.SelectedItem = item;
                return;
            }
        }
    }

    public void Dispose()
    {
        _selectionTimer?.Stop();
        _selectionTimer = null;
        try { _settingsRepo.Save(CaptureSettings()); } catch { /* best-effort */ }
        _projection.Dispose();
        _appCom.Dispose();
    }

    // ── Initialisation ────────────────────────────────────────────────────

    /// <summary>
    /// Populate UI elements that do NOT need an active project. Anything that
    /// requires _appCom belongs in OnWindowLoaded.
    /// </summary>
    private void InitializeStaticControls()
    {
        // Tab 1 (CmbPointsParentFolder is filled from the project in RefreshGroupCombos)
        TxtPointsFolderName.Text   = _settings.PointsFolderName;
        TxtPointsCount.Text        = _settings.PointsCount.ToString(CultureInfo.InvariantCulture);

        // Tab 2
        SelectComboItem(CmbCycleType, _settings.CycleType);

        // Tab 3 (CmbMeasuredParentFolder is filled from the project in RefreshGroupCombos)
        TxtMeasuredFolderName.Text   = _settings.MeasuredFolderName;
        TxtMeasuredFilePath.Text     = _settings.MeasuredFilePath;
        foreach (var parser in MeasuredPointsParserRegistry.All)
            CmbMeasuredParser.Items.Add(parser.DisplayName);
        SelectComboItem(CmbMeasuredParser, _settings.MeasuredParser);

        // Tab 4 (CmbDeviationPointsFolder is filled from the project in RefreshGroupCombos)
        foreach (var fmt in Enum.GetValues<RotationFormat>())
            CmbOutputFormat.Items.Add(fmt.ToString());
        SelectComboItem(CmbOutputFormat, _settings.OutputFormat);

        // Tab 5 (CmbTarget3DModel is filled from the project in RefreshGroupCombos)
        TxtLcsName.Text       = _settings.LcsName;
        SelectComboItem(CmbApplyMode, _settings.ApplyMode);
    }

    private WizardSettings CaptureSettings() => new()
    {
        PointsParentFolder   = CmbPointsParentFolder.SelectedItem as string ?? "",
        PointsFolderName     = TxtPointsFolderName.Text,
        PointsCount          = int.TryParse(TxtPointsCount.Text, out var n) ? n : 8,
        CycleType            = (CmbCycleType.SelectedItem as ComboBoxItem)?.Content?.ToString()
                               ?? _settings.CycleType,
        MeasuredParentFolder = CmbMeasuredParentFolder.SelectedItem as string ?? "",
        MeasuredFolderName   = TxtMeasuredFolderName.Text,
        MeasuredParser       = CmbMeasuredParser.SelectedItem as string ?? _settings.MeasuredParser,
        MeasuredFilePath     = TxtMeasuredFilePath.Text,
        DeviationPointsFolder = CmbDeviationPointsFolder.SelectedItem as string ?? "",
        DeviationNominalFolder = CmbDeviationNominalFolder.SelectedItem as string ?? "",
        OutputFormat          = CmbOutputFormat.SelectedItem as string ?? _settings.OutputFormat,
        ApplyMode            = (CmbApplyMode.SelectedItem as ComboBoxItem)?.Tag?.ToString()
                               ?? _settings.ApplyMode,
        LcsName              = TxtLcsName.Text,
        Target3DModelFolder  = CmbTarget3DModel.SelectedItem as string ?? "",
    };

    // ── Tab 1: Create Points ──────────────────────────────────────────────

    private async void BtnCreatePoints_Click(object sender, RoutedEventArgs e)
    {
        DbgLog.Write("BtnCreatePoints_Click enter");
        // Capture WPF-owned text on the UI thread BEFORE handing the action
        // off to Task.Run — TextBox.Text must not be read from a worker.
        var countText = TxtPointsCount.Text;
        var parent    = (CmbPointsParentFolder.SelectedItem as string ?? "").Trim();
        var folder    = TxtPointsFolderName.Text.Trim();

        await RunSafeAsync(TxtPointsStatus, () =>
        {
            DbgLog.Write("CreatePoints action: validating fields");
            if (!int.TryParse(countText, NumberStyles.Integer,
                              CultureInfo.InvariantCulture, out var n) || n <= 0)
                throw new ArgumentException("Number of points must be a positive integer.");
            // Empty parent = Model-page root, which is what most demos expect.
            if (folder.Length == 0) throw new ArgumentException("Folder name must not be empty.");

            DbgLog.Write("CreatePoints action: calling GetActiveProject");
            using var projCom = _appCom.GetActiveProject()
                ?? throw new InvalidOperationException("No active project — open a project first.");
            DbgLog.Write("CreatePoints action: sampling points");
            var pts = PointSamplingService.Sample(projCom, n);
            DbgLog.Write($"CreatePoints action: sampled {pts.Count} points, importing");
            GeomImportService.ImportPoints(projCom, parent, folder, pts);
            DbgLog.Write("CreatePoints action: import complete");
            return $"Created {pts.Count} points under '{parent}/{folder}'.";
        });
        // the new group should show up in the folder combos
        RefreshGroupCombos();
        DbgLog.Write("BtnCreatePoints_Click exit");
    }

    // ── Tab 2: Probing Cycles ─────────────────────────────────────────────

    private async void BtnCreateCycles_Click(object sender, RoutedEventArgs e)
    {
        var opId   = (CmbMeasurementOperation.SelectedItem as OperationOption)?.Id;
        var cycleS = (CmbCycleType.SelectedItem as ComboBoxItem)?.Content?.ToString()
                     ?? "SurfaceCycle";
        var cycleType = Enum.TryParse<ProbingCyclesService.CycleType>(cycleS, out var ct)
            ? ct
            : ProbingCyclesService.CycleType.SurfaceCycle;

        await RunSafeAsync(TxtCyclesStatus, () =>
        {
            using var projCom = _appCom.GetActiveProject()
                ?? throw new InvalidOperationException("No active project — open a project first.");
            int added = ProbingCyclesService.AddCyclesFromSelectedPoints(projCom, opId, cycleType);
            return $"Added {added} probing cycles.";
        });
        RefreshOperationsCombo();
    }

    private void BtnRefreshOperations_Click(object sender, RoutedEventArgs e) =>
        RefreshOperationsCombo();

    private async void BtnSetDefaultMachine_Click(object sender, RoutedEventArgs e)
    {
        await RunSafeAsync(TxtCyclesStatus, () => ProjectSetupService.SetDefaultMachine(_appCom));
        RefreshOperationsCombo();
    }

    private async void BtnCreateSetups_Click(object sender, RoutedEventArgs e)
    {
        await RunSafeAsync(TxtCyclesStatus, () =>
        {
            using var projCom = _appCom.GetActiveProject()
                ?? throw new InvalidOperationException("No active project — open a project first.");
            return ProjectSetupService.CreateSetups(projCom);
        });
        RefreshOperationsCombo();
    }

    private async void BtnCalculateNc_Click(object sender, RoutedEventArgs e)
    {
        await RunSafeAsync(TxtCyclesStatus, () => NcGenerationService.CalculateAndOpenNc(_appCom));
    }

    private void RefreshOperationsCombo()
    {
        CmbMeasurementOperation.Items.Clear();
        try
        {
            using var projCom = _appCom.GetActiveProject();
            if (projCom is null) return;
            CmbMeasurementOperation.Items.Add(
                new OperationOption(null, "<auto-create>"));
            foreach (var op in ProbingCyclesService.EnumerateProbingOperations(projCom))
                CmbMeasurementOperation.Items.Add(new OperationOption(op.Id, op.Caption));
            if (CmbMeasurementOperation.Items.Count > 0)
                CmbMeasurementOperation.SelectedIndex = 0;
        }
        catch
        {
            // headless / no project — leave combobox empty
        }
    }

    /// <summary>
    /// Fill the four "folder" combo boxes with the group (folder) nodes of the
    /// active project's geometry tree, then restore the last-used selections.
    /// Replaces the old "..." geometry picker — plain data, no host UI form.
    /// </summary>
    private void RefreshGroupCombos()
    {
        List<string> groups;
        try
        {
            using var projCom = _appCom.GetActiveProject();
            if (projCom is null) return;
            groups = GeomNodeLocator.ListGroupFullNames(projCom);
        }
        catch
        {
            return; // headless / no project — leave combos as they are
        }

        // Fall back to the persisted value only when there is no in-session
        // choice yet, so refreshing after Create/Import (which add new groups)
        // does not clobber what the user already selected.
        FillGroupCombo(CmbPointsParentFolder,    groups, _settings.PointsParentFolder);
        // Tab 3 defaults the parent folder to "Part" when nothing was chosen yet.
        FillGroupCombo(CmbMeasuredParentFolder,  groups,
            string.IsNullOrEmpty(_settings.MeasuredParentFolder) ? "Part" : _settings.MeasuredParentFolder);
        FillGroupCombo(CmbDeviationPointsFolder,  groups, _settings.DeviationPointsFolder);
        FillGroupCombo(CmbDeviationNominalFolder, groups, _settings.DeviationNominalFolder);
        FillGroupCombo(CmbTarget3DModel,          groups, _settings.Target3DModelFolder);
    }

    private static void FillGroupCombo(ComboBox combo, List<string> groups, string fallback)
    {
        var keep = combo.SelectedItem as string;
        combo.Items.Clear();
        foreach (var name in groups)
            combo.Items.Add(name);

        var want = !string.IsNullOrEmpty(keep) ? keep : fallback;
        foreach (string item in combo.Items)
            if (string.Equals(item, want, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        // no match — leave unselected rather than forcing a wrong folder
    }

    /// <summary>
    /// Update the Tab 1 "Selected surfaces: N" label from the current viewport
    /// selection. COM work runs on a worker thread (the plugin ALC resolves lazy
    /// CAMAPI assembly loads only off the STA thread — see RunSafeAsync), then the
    /// label is updated back on the UI thread.
    /// </summary>
    private async void UpdateSelectionCounters()
    {
        if (_selectionCountBusy) return; // skip if the previous query is still running
        _selectionCountBusy = true;
        int faces, points;
        try
        {
            (faces, points) = await Task.Run(() =>
            {
                using var projCom = _appCom.GetActiveProject();
                if (projCom is null) return (-1, -1);

                int f;
                using (var geomCom = projCom.CAMAPIGeomModel())
                using (var facesCom = geomCom.GetFaceListOfSelected())
                    f = facesCom.Invoke(list => list.Count);

                int p = 0;
                using (var geomCom = projCom.CAMAPIGeomModel())
                    foreach (var nodeCom in geomCom.EnumerateNodes())
                    {
                        if (!nodeCom.Invoke(n => n.Selected)) continue;
                        using var ent = nodeCom.GeometryEntity();
                        if (ent.IsNull) continue;
                        if (ent.EntityType() != CAMAPI.GeomModel.TCAMAPIGeometryEntityType.etPoint) continue;
                        p++;
                    }
                return (f, p);
            });
        }
        catch (Exception ex)
        {
            DbgLog.Write("UpdateSelectionCounters failed", ex);
            return;
        }
        finally
        {
            _selectionCountBusy = false;
        }

        SetCountLabel(TxtSelectedFacesCount, "surfaces", faces);
        SetCountLabel(TxtSelectedPointsCount, "points", points);
    }

    private static void SetCountLabel(TextBlock label, string noun, int count)
    {
        string key;
        if (count < 0)
        {
            label.Text = $"Selected {noun}: (no active project)";
            key = "ThemeMutedBrush";
        }
        else
        {
            label.Text = $"Selected {noun}: {count}";
            // dark orange when nothing is selected (reads better than red on dark)
            key = count == 0 ? "ThemeWarningBrush" : "ThemeOkBrush";
        }
        if (label.TryFindResource(key) is Brush brush)
            label.Foreground = brush;
    }

    /// <summary>Themed status brush by resource key, with a fallback.</summary>
    private Brush ThemedBrush(string key, Brush fallback) =>
        TryFindResource(key) as Brush ?? fallback;

    private sealed record OperationOption(string? Id, string Display)
    {
        public override string ToString() => Display;
    }

    // ── Tab 3: Import Measured Points ─────────────────────────────────────

    private async void BtnImportMeasured_Click(object sender, RoutedEventArgs e)
    {
        DbgLog.Write("BtnImportMeasured_Click enter");
        var parserName = CmbMeasuredParser.SelectedItem as string ?? "";
        var path       = TxtMeasuredFilePath.Text.Trim();
        var parent     = (CmbMeasuredParentFolder.SelectedItem as string ?? "").Trim();
        var folder     = TxtMeasuredFolderName.Text.Trim();

        await RunSafeAsync(TxtMeasuredStatus, () =>
        {
            DbgLog.Write($"ImportMeasured: parser='{parserName}', file='{path}', folder='{folder}'");
            var parser = MeasuredPointsParserRegistry.FindByDisplayName(parserName)
                         ?? throw new ArgumentException("Choose a file parser.");
            if (!File.Exists(path))
                throw new FileNotFoundException("Measured report file not found.", path);
            if (folder.Length == 0) throw new ArgumentException("Folder name must not be empty.");

            using var projCom = _appCom.GetActiveProject()
                ?? throw new InvalidOperationException("No active project — open a project first.");
            var pts = parser.Parse(path);
            if (pts.Count == 0) throw new InvalidDataException("File contains no points.");
            DbgLog.Write($"ImportMeasured: parsed {pts.Count} points, importing");
            GeomImportService.ImportPoints(projCom, parent, folder, pts);
            DbgLog.Write("ImportMeasured: import complete");
            return $"Imported {pts.Count} points from {Path.GetFileName(path)}.";
        });
        // the new group should show up in the folder combos
        RefreshGroupCombos();
        DbgLog.Write("BtnImportMeasured_Click exit");
    }

    private void BtnBrowseMeasuredFile_Click(object sender, RoutedEventArgs e)
    {
        var parser = MeasuredPointsParserRegistry.FindByDisplayName(
            CmbMeasuredParser.SelectedItem as string ?? "");
        var filter = parser is null
            ? "All files (*.*)|*.*"
            : $"{parser.DisplayName}|{parser.FileFilter}|All files (*.*)|*.*";
        var dlg = new OpenFileDialog { Filter = filter };
        if (!string.IsNullOrEmpty(TxtMeasuredFilePath.Text))
            dlg.InitialDirectory = Path.GetDirectoryName(TxtMeasuredFilePath.Text) ?? "";
        if (dlg.ShowDialog(this) == true)
            TxtMeasuredFilePath.Text = dlg.FileName;
    }

    private async void BtnEmulateMeasured_Click(object sender, RoutedEventArgs e)
    {
        var path = Path.Combine(Path.GetTempPath(),
            $"PartCalib_emulated_measured_{Environment.ProcessId}.txt");
        // Read the offset fields on the UI thread before handing off to Task.Run.
        var txText = TxtEmulateTX.Text;
        var tyText = TxtEmulateTY.Text;
        var tzText = TxtEmulateTZ.Text;
        var rxText = TxtEmulateRX.Text;
        var ryText = TxtEmulateRY.Text;
        var rzText = TxtEmulateRZ.Text;
        await RunSafeAsync(TxtMeasuredStatus, () =>
        {
            if (!double.TryParse(txText, NumberStyles.Float, CultureInfo.InvariantCulture, out var tx) ||
                !double.TryParse(tyText, NumberStyles.Float, CultureInfo.InvariantCulture, out var ty) ||
                !double.TryParse(tzText, NumberStyles.Float, CultureInfo.InvariantCulture, out var tz) ||
                !double.TryParse(rxText, NumberStyles.Float, CultureInfo.InvariantCulture, out var rx) ||
                !double.TryParse(ryText, NumberStyles.Float, CultureInfo.InvariantCulture, out var ry) ||
                !double.TryParse(rzText, NumberStyles.Float, CultureInfo.InvariantCulture, out var rz))
                throw new ArgumentException("Move/Rotate values must be valid numbers.");
            var n = WriteEmulatedMeasuredFile(_appCom, path, tx, ty, tz, rx, ry, rz);
            return $"Emulated {n} measured points (move {tx};{ty};{tz} mm, rot {rx};{ry};{rz}°) → {Path.GetFileName(path)}";
        });
        // wire the generated file up so the user can import it straight away
        if (File.Exists(path))
        {
            TxtMeasuredFilePath.Text = path;
            SelectComboItem(CmbMeasuredParser, "Plain text (X;Y;Z)");
        }
    }

    private void BtnViewMeasuredFile_Click(object sender, RoutedEventArgs e)
    {
        var path = TxtMeasuredFilePath.Text.Trim();
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            TxtMeasuredStatus.Foreground = ThemedBrush("ThemeWarningBrush", Brushes.Orange);
            TxtMeasuredStatus.Text = "No measured file to view — browse or Emulate one first.";
            return;
        }
        try
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            TxtMeasuredStatus.Foreground = ThemedBrush("ThemeErrorBrush", Brushes.Red);
            TxtMeasuredStatus.Text = $"Cannot open file: {ex.Message}";
        }
    }

    // ── Tab 4: Calculate Deviation ────────────────────────────────────────

    private async void BtnCalculateDeviation_Click(object sender, RoutedEventArgs e)
    {
        var folder  = (CmbDeviationPointsFolder.SelectedItem as string ?? "").Trim();
        var nominalFolder = (CmbDeviationNominalFolder.SelectedItem as string ?? "").Trim();
        var fmtName = CmbOutputFormat.SelectedItem as string ?? "EulerZYX";

        await RunSafeAsync(TxtDeviationStatus, () =>
        {
            if (folder.Length == 0) throw new ArgumentException("Measured points folder must not be empty.");

            using var projCom = _appCom.GetActiveProject()
                ?? throw new InvalidOperationException("No active project — open a project first.");
            var res = _deviation.Calculate(projCom, folder, nominalFolder, _projection);

            var fmt = Enum.TryParse<RotationFormat>(fmtName, out var f) ? f : RotationFormat.EulerZYX;
            var text = RotationFormatter.Format(res.Matrix, fmt);

            _lastResult = res;
            _lastFormattedResult = text;
            Dispatcher.Invoke(() =>
            {
                TxtDeviationResult.Text = text;
                TxtDeviationMax.Text = res.MaxResidual.ToString("F6", CultureInfo.InvariantCulture);
                TxtApplyPreview.Text = text;
            });
            return $"Calibration computed on {res.PointCount} points; max residual {res.MaxResidual:F4} mm.";
        });
    }

    // ── Tab 5: Apply Result ───────────────────────────────────────────────

    private void CmbApplyMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PanelLcsName is null) return; // during initial XAML loading
        var mode = (CmbApplyMode.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "CreateLCS";
        PanelLcsName.Visibility   = mode == "CreateLCS"      ? Visibility.Visible : Visibility.Collapsed;
        PanelPartStage.Visibility = mode == "MovePart"       ? Visibility.Visible : Visibility.Collapsed;
        Panel3DModel.Visibility   = mode == "TransformModel" ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_lastResult is null)
        {
            TxtApplyStatus.Foreground = ThemedBrush("ThemeErrorBrush", Brushes.Red);
            TxtApplyStatus.Text = "Run 'Calculate deviation' on tab 4 first.";
            return;
        }
        var mode    = (CmbApplyMode.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? "CreateLCS";
        var matrix  = _lastResult.Matrix;
        var lcsName = TxtLcsName.Text.Trim();
        var partRef = CmbPartStage.SelectedItem as ApplyResultService.PartStageRef;
        var target  = (CmbTarget3DModel.SelectedItem as string ?? "").Trim();

        await RunSafeAsync(TxtApplyStatus, () =>
        {
            using var projCom = _appCom.GetActiveProject()
                ?? throw new InvalidOperationException("No active project — open a project first.");

            switch (mode)
            {
                case "CreateLCS":
                    ApplyResultService.CreateLcs(projCom, matrix, lcsName);
                    return $"LCS '{lcsName}' created.";

                case "MovePart":
                    if (partRef is null) throw new ArgumentException("Choose a PartStage.");
                    ApplyResultService.MovePartStage(projCom, matrix, partRef);
                    return $"Workpiece setup of {partRef}: reused Setup 1 CS, offset adjusted by calibration.";

                case "TransformModel":
                    ApplyResultService.Transform3DModel(projCom, matrix, target);
                    return $"3D model '{target}' transformed.";

                default:
                    throw new InvalidOperationException($"Unknown apply mode '{mode}'.");
            }
        });
    }

    private void BtnCopyResult_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_lastFormattedResult))
        {
            TxtApplyStatus.Foreground = ThemedBrush("ThemeErrorBrush", Brushes.Red);
            TxtApplyStatus.Text = "Nothing to copy. Run 'Calculate deviation' first.";
            return;
        }
        Clipboard.SetText(_lastFormattedResult);
        TxtApplyStatus.Foreground = ThemedBrush("ThemeOkBrush", Brushes.DarkGreen);
        TxtApplyStatus.Text = "Result copied to clipboard.";
    }

    private void BtnRefreshPartStages_Click(object sender, RoutedEventArgs e) =>
        RefreshPartStagesCombo();

    private void RefreshPartStagesCombo()
    {
        CmbPartStage.Items.Clear();
        try
        {
            using var projCom = _appCom.GetActiveProject();
            if (projCom is null) return;
            foreach (var s in ApplyResultService.EnumeratePartStages(projCom))
                CmbPartStage.Items.Add(s);
            if (CmbPartStage.Items.Count > 0) CmbPartStage.SelectedIndex = 0;
        }
        catch
        {
            // No project / headless — silently ignore.
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task RunSafeAsync(TextBox statusBlock, Func<string?> action)
    {
        // Run on Task.Run worker. Counter-intuitively this is more reliable
        // than the UI/STA thread for CAMAPI work because the plugin ALC
        // appears to delegate lazy assembly loads (CAMAPI.Project,
        // STGeomApiTypes, ...) cleanly only from a worker apartment; touching
        // them first from the window's STA raises FileLoadException
        // 0x80131509. Keep the UI thread for capturing TextBox.Text before
        // dispatching — that's what callers already do.
        SetBusy(true);
        try
        {
            DbgLog.Write("RunSafeAsync: dispatching to Task.Run");
            var msg = await Task.Run(() =>
            {
                try { return action(); }
                catch (Exception ex)
                {
                    DbgLog.Write("RunSafeAsync inner action failed", ex);
                    throw;
                }
            });
            if (msg != null)
            {
                statusBlock.Foreground = ThemedBrush("ThemeOkBrush", Brushes.DarkGreen);
                statusBlock.Text = msg;
            }
        }
        catch (Exception ex)
        {
            DbgLog.Write("RunSafeAsync outer caught", ex);
            statusBlock.Foreground = ThemedBrush("ThemeErrorBrush", Brushes.Red);
            statusBlock.Text = $"Error: {ex.Message}";
        }
        finally
        {
            SetBusy(false);
            try { _settingsRepo.Save(CaptureSettings()); } catch { /* best-effort */ }
        }
    }

    private void SetBusy(bool busy)
    {
        BtnCreatePoints.IsEnabled    = !busy;
        BtnCreateCycles.IsEnabled    = !busy;
        BtnImportMeasured.IsEnabled  = !busy;
        BtnCalculateDeviation.IsEnabled = !busy;
        BtnApply.IsEnabled           = !busy;
        BusyIndicator.IsIndeterminate = busy;
        BusyIndicator.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void SelectComboItem(ComboBox combo, string value)
    {
        foreach (var item in combo.Items)
        {
            string? itemValue = item switch
            {
                ComboBoxItem c => (c.Tag as string) ?? c.Content?.ToString(),
                string s       => s,
                _              => item?.ToString(),
            };
            if (string.Equals(itemValue, value, StringComparison.OrdinalIgnoreCase))
            {
                combo.SelectedItem = item;
                return;
            }
        }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

}
