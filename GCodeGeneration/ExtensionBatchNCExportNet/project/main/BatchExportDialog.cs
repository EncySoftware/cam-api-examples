using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ExtensionBatchNCExportNet;

internal sealed class BatchExportDialog : Form
{
    private readonly TextBox search = new() { Dock = DockStyle.Fill, PlaceholderText = "Search by name or subfolder" };
    private readonly ListBox processors = new() { Dock = DockStyle.Fill, HorizontalScrollbar = true };
    private readonly TextBox settings = new() { Dock = DockStyle.Fill, PlaceholderText = "Embedded DLL settings (or select XML)" };
    private readonly ComboBox destination = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList, DropDownWidth = 740 };
    private readonly bool linkedToPlm;
    private readonly ComboBox mode = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox output = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 48, ScrollBars = ScrollBars.Vertical, BackColor = SystemColors.Window };
    private readonly Label notice = new() { Dock = DockStyle.Fill, AutoEllipsis = true };
    private readonly Button settingsBrowse = new() { Text = "DLL settings…", AutoSize = true };
    private readonly Button start = new() { Text = "Generate and export", AutoSize = true };
    private readonly List<Item> items = [];
    private readonly Dictionary<string, string> settingsByProcessor = new(StringComparer.OrdinalIgnoreCase);
    private string? settingsProcessorPath;


    private readonly string standardFolder;
    private string outputRoot;
    private BatchExportOptions? result;

    private sealed record Item(string Path, string Caption, string SettingsPath)
    {
        public override string ToString() => Caption;
    }

    internal BatchExportDialog(ExportTargetList targets, bool linkedToPlm, string standardFolder, string projectName,
        string projectKey, bool byGroups, PostprocessorHistory history)
    {


        this.standardFolder = standardFolder;
        this.linkedToPlm = linkedToPlm;

        outputRoot = BatchExportFiles.DefaultOutputRoot(projectName);
        Text = "Batch NC generation and export";
        Font = new Font("Segoe UI", 10);
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(800, 710);
        MinimumSize = new Size(700, 680);
        StartPosition = FormStartPosition.CenterScreen;
        MinimizeBox = false;
        MaximizeBox = false;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 2, RowCount = 11 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        for (var i = 0; i < 11; i++)
            layout.RowStyles.Add(new RowStyle(i == 4 ? SizeType.Percent : SizeType.AutoSize, i == 4 ? 100 : 0));
        Controls.Add(layout);
        void Wide(Control c, int row) { layout.Controls.Add(c, 0, row); layout.SetColumnSpan(c, 2); }
        var targetPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1 };
        targetPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        targetPanel.Controls.Add(new Label { Text = "Export to", AutoSize = true });
        destination.Items.AddRange(targets.Targets.Cast<object>().ToArray());
        if (history.Targets?.TryGetValue(projectKey, out var savedTarget) == true)
            destination.SelectedIndex = targets.Targets.FindIndex(t => t.ExporterId == savedTarget.ExporterId && t.TargetIndex == savedTarget.TargetIndex);
        if (destination.SelectedIndex < 0 && targets.Targets.Count == 1) destination.SelectedIndex = 0;
        targetPanel.Controls.Add(destination);
        if (!string.IsNullOrWhiteSpace(targets.Message))
            targetPanel.Controls.Add(new TextBox { Text = targets.Message.Trim(), ReadOnly = true, Multiline = true,
                ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Height = 48, BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Control, TabStop = false });
        Wide(targetPanel, 0);
        destination.SelectedIndexChanged += (_, _) => UpdateStart();
        mode.Items.AddRange(["By operation", "By operation group"]);
        mode.SelectedIndex = byGroups ? 1 : 0;
        Wide(mode, 1);
        Wide(new Label { Text = "Postprocessor", AutoSize = true }, 2);
        layout.Controls.Add(search, 0, 3);
        var browse = new Button { Text = "Browse…", AutoSize = true };
        layout.Controls.Add(browse, 1, 3);
        Wide(processors, 4);
        layout.Controls.Add(settings, 0, 5);
        layout.Controls.Add(settingsBrowse, 1, 5);
        Wide(notice, 6);
        notice.MinimumSize = new Size(0, 40);
        var outputBox = new GroupBox { Text = "NC output folder", Dock = DockStyle.Fill, Height = 80, Padding = new Padding(8) };
        outputBox.Controls.Add(output);
        layout.Controls.Add(outputBox, 0, 7);
        output.MinimumSize = new Size(0, 45);
        var changeFolder = new Button { Text = "Change folder…", AutoSize = true };
        layout.Controls.Add(changeFolder, 1, 7);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft };
        var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
        buttons.Controls.Add(cancel);
        buttons.Controls.Add(start);
        if (history.Results?.TryGetValue(projectKey, out var lastResult) == true)
        {
            var lastPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2 };
            lastPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            lastPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            lastPanel.Controls.Add(new TextBox { Text = "Last result: " + lastResult.Summary + Environment.NewLine + lastResult.Folder,
                Multiline = true, ReadOnly = true, Dock = DockStyle.Fill, Height = 55, ScrollBars = ScrollBars.Vertical }, 0, 0);
            var openLast = new Button { Text = "Open…", AutoSize = true };
            openLast.Click += (_, _) => ShowResult(lastResult.Summary, lastResult.Folder);
            lastPanel.Controls.Add(openLast, 1, 0);
            Wide(lastPanel, 9);
        }
        Wide(buttons, 10);
        AcceptButton = start;
        CancelButton = cancel;
        output.Text = outputRoot;

        if (history.Projects.TryGetValue(projectKey, out var previous) && File.Exists(previous.FilePath))
            Add(previous, "This project · ");
        foreach (var recent in history.Recent.Where(p => File.Exists(p.FilePath)))
            Add(recent, "Recent · ");
        foreach (var file in BatchExportFiles.Discover(standardFolder))
            Add(new(file), "");
        search.TextChanged += (_, _) => RefreshList();
        processors.SelectedIndexChanged += (_, _) => SelectProcessor();
        browse.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "Postprocessors (*.sppx;*.dll)|*.sppx;*.dll", InitialDirectory = standardFolder, CheckFileExists = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            Add(new(dialog.FileName), "Custom file · ");
            search.Clear();
            RefreshList(dialog.FileName);
        };
        settingsBrowse.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog { Filter = "Postprocessor settings (*.xml)|*.xml", CheckFileExists = true };
            if (dialog.ShowDialog(this) == DialogResult.OK) settings.Text = dialog.FileName;
        };
        changeFolder.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog { Description = "Batch export output folder", UseDescriptionForTitle = true };
            if (dialog.ShowDialog(this) != DialogResult.OK) return;
            outputRoot = dialog.SelectedPath;
            output.Text = outputRoot;
        };
        start.Click += (_, _) => Confirm();
        var preferred = history.Projects.TryGetValue(projectKey, out var saved) && File.Exists(saved.FilePath)
            ? saved.FilePath : history.Recent.FirstOrDefault(p => File.Exists(p.FilePath))?.FilePath;
        RefreshList(preferred);
    }

    private void Add(PostprocessorChoice choice, string prefix)
    {
        if (!BatchExportFiles.IsPostprocessor(choice.FilePath) || items.Any(i => i.Path.Equals(choice.FilePath, StringComparison.OrdinalIgnoreCase))) return;
        var relative = string.IsNullOrWhiteSpace(standardFolder) ? choice.FilePath : Path.GetRelativePath(standardFolder, choice.FilePath);
        items.Add(new(choice.FilePath, prefix + Path.GetFileName(choice.FilePath) + " — " + Path.GetDirectoryName(relative), choice.SettingsPath));
    }

    private void RefreshList(string? selected = null)
    {
        selected ??= (processors.SelectedItem as Item)?.Path;
        var matches = items.Where(i => i.Caption.Contains(search.Text.Trim(), StringComparison.CurrentCultureIgnoreCase)).ToArray();
        processors.BeginUpdate();
        processors.Items.Clear();
        processors.Items.AddRange(matches);
        processors.SelectedIndex = Array.FindIndex(matches, i => i.Path == selected);
        processors.EndUpdate();
        SelectProcessor();
    }

    private void SelectProcessor()
    {
        var item = processors.SelectedItem as Item;
        UpdateStart();
        var dotnet = item != null && Path.GetExtension(item.Path).Equals(".dll", StringComparison.OrdinalIgnoreCase);
        settings.Enabled = settingsBrowse.Enabled = dotnet;
        // Preserve manual input and an empty path (embedded settings).
        // Filtering temporarily clears the selection but must preserve the selected XML.
        if (settingsProcessorPath != null)
            settingsByProcessor[settingsProcessorPath] = settings.Text;
        settingsProcessorPath = dotnet ? item!.Path : null;
        if (settingsProcessorPath != null)
        {
            if (!settingsByProcessor.TryGetValue(settingsProcessorPath, out var selectedSettings))
                selectedSettings = File.Exists(item!.SettingsPath) ? item.SettingsPath : BatchExportFiles.FindSettings(item.Path);
            settings.Text = selectedSettings;
        }
        else
            settings.Text = "";
        notice.Text = item == null ? (processors.Items.Count == 0 ? "No postprocessors found. Change the search or click Browse." : "Select a postprocessor suitable for the machine.") :
            dotnet ? "Uses a copy of Settings.xml. Settings from the standard generation window are not imported." : item.Path;
    }

    private void UpdateStart() => start.Enabled = linkedToPlm && processors.SelectedItem is Item && destination.SelectedItem is ExportTarget;

    private void Confirm()
    {
        try
        {
            if (!linkedToPlm)
                throw new InvalidOperationException("To export NC programs, open a project from PLM and run the utility again.");
            if (destination.SelectedItem is not ExportTarget target)
                throw new InvalidOperationException("Select an NC exporter.");
            if (processors.SelectedItem is not Item item || !File.Exists(item.Path))
                throw new InvalidOperationException("Select an existing postprocessor.");
            if (Path.GetExtension(item.Path).Equals(".dll", StringComparison.OrdinalIgnoreCase))
                BatchExportFiles.ReadDotnetSettings(settings.Text, item.Path);
            result = new(new(item.Path, settings.Text), mode.SelectedIndex == 1, outputRoot, target);
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Export settings", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    internal static BatchExportOptions? Choose(ExportTargetList targets, bool linkedToPlm, string standardFolder, string projectName,
        string projectKey, bool byGroups, PostprocessorHistory history) => InDialogThread(() =>
    {
        using var dialog = new BatchExportDialog(targets, linkedToPlm, standardFolder, projectName, projectKey, byGroups, history);
        return dialog.ShowDialog() == DialogResult.OK ? dialog.result : null;
    });

    internal static void ShowResult(string message, string folder) => InDialogThread(() =>
    {
        var shortMessage = message.Split(new[] { " Results:" }, StringSplitOptions.None)[0].Trim();
        using var form = new Form
        {
            Text = "Batch export result",
            ClientSize = new Size(500, 145),
            MinimumSize = new Size(500, 145),
            StartPosition = FormStartPosition.CenterScreen,
            Font = new Font("Segoe UI", 10),
            MinimizeBox = false,
            MaximizeBox = false
        };
        form.Controls.Add(new Label { Dock = DockStyle.Fill, AutoSize = false, Text = shortMessage, Padding = new Padding(12, 12, 12, 4) });
        var panel = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 42, Padding = new Padding(8, 4, 8, 4) };
        var openFolder = new Button { Text = "Open output folder", AutoSize = true, Enabled = Directory.Exists(folder) };
        var logPath = Path.Combine(folder, "export.log");
        var openLog = new Button { Text = "Open log", AutoSize = true, Enabled = File.Exists(logPath) };
        void Open(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(form, ex.Message); }
        }
        openFolder.Click += (_, _) => Open(folder);
        openLog.Click += (_, _) => Open(logPath);
        var close = new Button { Text = "Close", DialogResult = DialogResult.OK, AutoSize = true };
        panel.Controls.Add(openFolder);
        panel.Controls.Add(openLog);
        panel.Controls.Add(close);
        form.Controls.Add(panel);
        form.AcceptButton = form.CancelButton = close;
        form.ShowDialog();
        return true;
    });
    private static T InDialogThread<T>(Func<T> action)
    {
        if (Thread.CurrentThread.GetApartmentState() == ApartmentState.STA) return action();
        T value = default!;
        Exception? error = null;
        var thread = new Thread(() => { try { value = action(); } catch (Exception ex) { error = ex; } });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        if (error != null) throw new InvalidOperationException("Could not open the export window.", error);
        return value;
    }
}
