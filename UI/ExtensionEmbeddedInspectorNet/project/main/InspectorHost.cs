using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using CAMAPI.DotnetHelper;
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;
using STCustomPropTypes;

namespace ExtensionEmbeddedInspectorNet;

/// <summary>
/// WPF element showing the ENCY inspector. This element lives on the thread of its WPF window, while
/// the inspector is a native ENCY window living on the ENCY main thread: it is created, called and
/// released there, and its property getters, setters and the OK / Cancel callback run there too
/// </summary>
public sealed class InspectorHost : HwndHost
{
    private const int WS_CHILD = 0x40000000;
    private const int WS_VISIBLE = 0x10000000;
    private const int WS_CLIPCHILDREN = 0x02000000;
    private const uint GW_CHILD = 5;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_ASYNCWINDOWPOS = 0x4000;

    private readonly EncyMainThread _mainThread;
    private readonly Func<SimplePropIterator> _buildProperties;
    private readonly OnDialogWindowClose _onClose;
    private IntPtr _container;

    // Used only on the ENCY main thread
    private SimplePropIterator? _properties;
    private ICAMAPI_UIDialogWindow? _inspector;

    /// <summary>
    /// Inspector with the OK / Cancel bar
    /// </summary>
    /// <param name="mainThread">Runs actions on the ENCY main thread</param>
    /// <param name="buildProperties">Builds the properties to show; called on the ENCY main thread</param>
    /// <param name="onClose">Gets the button pressed in the inspector bar; called on the ENCY main thread</param>
    public InspectorHost(EncyMainThread mainThread, Func<SimplePropIterator> buildProperties,
        OnDialogWindowClose onClose)
    {
        _mainThread = mainThread;
        _buildProperties = buildProperties;
        _onClose = onClose;
    }

    /// <summary>
    /// Releases the inspector on the ENCY main thread, then calls <paramref name="onReleased"/>
    /// on the thread of this element. The window must not close before that
    /// </summary>
    public void Release(Action onReleased)
    {
        _mainThread.Post(() =>
        {
            try
            {
                if (_inspector != null)
                    Marshal.FinalReleaseComObject(_inspector);
                _inspector = null;
                _properties?.Dispose();
                _properties = null;
            }
            finally
            {
                Dispatcher.BeginInvoke(onReleased);
            }
        });
    }

    /// <inheritdoc />
    protected override HandleRef BuildWindowCore(HandleRef hwndParent)
    {
        // ENCY sizes the inspector to its parent only once, so it gets a container that this element resizes
        _container = CreateWindowEx(0, "STATIC", "", WS_CHILD | WS_VISIBLE | WS_CLIPCHILDREN,
            0, 0, 0, 0, hwndParent.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        if (_container == IntPtr.Zero)
            throw new InvalidOperationException("Cannot create the inspector container window");

        var container = _container;
        _mainThread.Post(() => CreateInspector(container));
        return new HandleRef(this, _container);
    }

    /// <inheritdoc />
    protected override void OnWindowPositionChanged(Rect rcBoundingBox)
    {
        base.OnWindowPositionChanged(rcBoundingBox);

        // Asynchronous, so this thread never waits for a busy ENCY
        FitInspector(_container, SWP_ASYNCWINDOWPOS);
    }

    /// <inheritdoc />
    protected override void DestroyWindowCore(HandleRef hwnd)
    {
        DestroyWindow(hwnd.Handle);
        _container = IntPtr.Zero;
    }

    /// <summary>
    /// Runs on the ENCY main thread
    /// </summary>
    private void CreateInspector(IntPtr container)
    {
        if (!IsWindow(container))
            return;

        _properties = _buildProperties();
        using var helperCom = UIDialogs.CreateHelper();
        _inspector = DirectCall(helperCom).CreateEmbeddedWindow("", (long)container);
        _inspector.PropIteratorGetter = new PropIteratorGetter(GetProperties);
        _inspector.OnClose = new CamApiInspectorWindowOnClose { OnCloseDelegate = _onClose };
        _inspector.Show();
        FitInspector(container, 0);
    }

    /// <summary>
    /// Properties from the root, each time the inspector rebuilds its rows. Runs on the ENCY main thread
    /// </summary>
    private IST_CustomPropIterator GetProperties()
    {
        var properties = _properties ?? throw new InvalidOperationException("The inspector is released");
        properties.MoveToRoot();
        return (IST_CustomPropIterator)DirectCall(properties.Iterator);
    }

    /// <summary>
    /// Stretches the inspector, the only child of the container, over the container
    /// </summary>
    private static void FitInspector(IntPtr container, uint flags)
    {
        var inspectorWindow = GetWindow(container, GW_CHILD);
        if (inspectorWindow != IntPtr.Zero && GetClientRect(container, out var rect))
            SetWindowPos(inspectorWindow, IntPtr.Zero, 0, 0, rect.Right, rect.Bottom,
                SWP_NOZORDER | SWP_NOACTIVATE | flags);
    }

    /// <summary>
    /// COM object to call on the current thread. Invoke is not used: after WindowHelper.ShowStaWindow
    /// it runs calls on a pool thread, and the inspector works only on the ENCY main thread
    /// </summary>
    private static T DirectCall<T>(ComWrapper<T> wrapper)
    {
#pragma warning disable CS0618
        return wrapper.Instance ?? throw new InvalidOperationException($"{typeof(T).Name} is not available");
#pragma warning restore CS0618
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hwnd, uint command);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hwnd, out NativeRect rect);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int width, int height,
        uint flags);
}
