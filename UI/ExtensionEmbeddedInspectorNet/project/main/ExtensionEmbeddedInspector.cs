using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

namespace ExtensionEmbeddedInspectorNet;

/// <summary>
/// Utility that opens its own window with the ENCY inspector inside. ENCY creates a new instance
/// for every run, so the state shared between runs is static; unload_mode in the settings file
/// keeps the library, and with it this state, loaded until ENCY closes
/// </summary>
public class ExtensionEmbeddedInspector : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private static volatile StockWindow? _window;
    private static volatile bool _windowOpen;
    private static StockValues _values = new();

    /// <inheritdoc />
    public IExtensionInfo? Info { get; set; }

    /// <inheritdoc />
    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            if (_windowOpen)
            {
                var window = _window;
                window?.Dispatcher.BeginInvoke(window.Activate);
                return;
            }

            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var mainFormCom = applicationCom.MainForm();
            var mainWindowHandle = mainFormCom.MainWindowHandle();
            var palette = ThemePalette.Read(applicationCom);

            // Run is called on the ENCY main thread, so the inspector is created and released through this object
            var mainThread = new EncyMainThread();
            var values = _values.Copy();
            _windowOpen = true;

            void OnWindowGone()
            {
                _window = null;
                _windowOpen = false;
                mainThread.Post(mainThread.Dispose);
            }

            WindowHelper.ShowStaWindow(mainWindowHandle,
                () =>
                {
                    var window = new StockWindow(values, palette, mainThread);
                    window.Applied += applied => _values = applied;
                    _window = window;
                    return window;
                },
                OnWindowGone,
                exception =>
                {
                    OnWindowGone();
                    System.Windows.MessageBox.Show(exception.Message, "Stock");
                });
        }
        catch (Exception e)
        {
            _windowOpen = false;
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>
    /// Allow unloading only when the window is closed
    /// </summary>
    public bool CanUnload
    {
        get => !_windowOpen;
        set { }
    }
}
