using System.ComponentModel;
using System.Windows;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

namespace ExtensionEmbeddedInspectorNet;

/// <summary>
/// Extension window with the ENCY inspector inside. The window runs on its own STA thread
/// </summary>
public partial class StockWindow : Window, IDisposable
{
    // Used only on the ENCY main thread, where the inspector reads and writes it
    private StockValues _draft;

    private readonly EncyMainThread _mainThread;
    private readonly InspectorHost _inspector;
    private bool _releasing;
    private bool _released;

    /// <summary>
    /// The user pressed OK; gets the edited values. Raised on the ENCY main thread
    /// </summary>
    public event Action<StockValues>? Applied;

    /// <summary>
    /// Window editing a copy of the values
    /// </summary>
    public StockWindow(StockValues values, ThemePalette palette, EncyMainThread mainThread)
    {
        InitializeComponent();
        palette.ApplyTo(Resources);

        _mainThread = mainThread;
        _draft = values.Copy();
        SummaryText.Text = _draft.ToString();
        _inspector = new InspectorHost(mainThread, BuildProperties, OnInspectorButton);
        InspectorPlace.Child = _inspector;
    }

    /// <summary>
    /// Inspector rows bound to the draft values. Runs on the ENCY main thread
    /// </summary>
    private SimplePropIterator BuildProperties()
    {
        var properties = new SimplePropIterator();
        properties.AddStringProp("Name",
            () => _draft.Caption,
            value => Edit(() => _draft.Caption = value));

        var size = properties.AddComplexProp("Size");
        size.AddDoubleProp("Length",
            () => _draft.Length,
            value => Edit(() => _draft.Length = value));
        size.AddDoubleProp("Width",
            () => _draft.Width,
            value => Edit(() => _draft.Width = value));
        size.AddDoubleProp("Height",
            () => _draft.Height,
            value => Edit(() => _draft.Height = value));

        properties.AddEnumIndexedProp("Material",
            () => _draft.MaterialIndex,
            value => Edit(() => _draft.MaterialIndex = value),
            StockValues.Materials);
        properties.AddBooleanProp("Show in 3D view",
            () => _draft.Visible,
            value => Edit(() => _draft.Visible = value));
        return properties;
    }

    /// <summary>
    /// Changes the draft on the ENCY main thread and shows the result on the thread of this window
    /// </summary>
    private void Edit(Action change)
    {
        change();
        var summary = _draft.ToString();
        Dispatcher.BeginInvoke(() => SummaryText.Text = summary);
    }

    /// <summary>
    /// Restores the default values on the ENCY main thread and shows them in the inspector
    /// </summary>
    private void OnResetClick(object sender, RoutedEventArgs e)
    {
        _mainThread.Post(() =>
        {
            Edit(() => _draft = new StockValues());
            _inspector.Refresh();
        });
    }

    /// <summary>
    /// Runs on the ENCY main thread
    /// </summary>
    private void OnInspectorButton(TUIButtonType button)
    {
        if (button == TUIButtonType.btOk)
            Applied?.Invoke(_draft.Copy());
        Dispatcher.BeginInvoke(Close);
    }

    /// <inheritdoc />
    protected override void OnClosing(CancelEventArgs e)
    {
        base.OnClosing(e);
        if (e.Cancel || _released)
            return;

        // The inspector is released on the ENCY main thread first, then the window closes for real
        e.Cancel = true;
        if (_releasing)
            return;
        _releasing = true;
        _inspector.Release(() =>
        {
            _released = true;
            Close();
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _inspector.Dispose();
    }
}
