using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ExtensionEmbeddedInspectorNet;

/// <summary>
/// Runs actions on the ENCY main thread from any thread. Create it on the ENCY main thread
/// (in IExtensionUtility.Run) and dispose it there too. It is a hidden message window, so ENCY
/// runs the actions from its own message loop
/// </summary>
public sealed class EncyMainThread : IDisposable
{
    private const uint WM_RUN_ACTIONS = 0x8000 + 1;
    private static readonly IntPtr HWND_MESSAGE = new(-3);

    private readonly ConcurrentQueue<Action> _actions = new();
    private readonly WindowProcedure _windowProcedure;
    private readonly string _className = "EncyMainThread_" + Guid.NewGuid().ToString("N");
    private readonly IntPtr _instance = GetModuleHandle(null);
    private IntPtr _window;

    /// <summary>
    /// Binds to the current thread, which must be the ENCY main thread
    /// </summary>
    public EncyMainThread()
    {
        _windowProcedure = OnMessage;
        var windowClass = new WindowClass
        {
            WindowProcedure = Marshal.GetFunctionPointerForDelegate(_windowProcedure),
            Instance = _instance,
            ClassName = _className
        };
        if (RegisterClass(ref windowClass) == 0)
            throw new InvalidOperationException("Cannot register the main thread window class");
        _window = CreateWindowEx(0, _className, "", 0, 0, 0, 0, 0, HWND_MESSAGE, IntPtr.Zero, _instance, IntPtr.Zero);
        if (_window == IntPtr.Zero)
            throw new InvalidOperationException("Cannot create the main thread window");
    }

    /// <summary>
    /// Queues the action; ENCY runs it on its main thread as soon as it handles messages
    /// </summary>
    public void Post(Action action)
    {
        _actions.Enqueue(action);
        PostMessage(_window, WM_RUN_ACTIONS, IntPtr.Zero, IntPtr.Zero);
    }

    /// <summary>
    /// Destroys the hidden window. Call it on the ENCY main thread, for example through <see cref="Post"/>
    /// </summary>
    public void Dispose()
    {
        if (_window == IntPtr.Zero)
            return;
        DestroyWindow(_window);
        _window = IntPtr.Zero;
        UnregisterClass(_className, _instance);
    }

    private IntPtr OnMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam)
    {
        if (message != WM_RUN_ACTIONS)
            return DefWindowProc(hwnd, message, wParam, lParam);

        while (_actions.TryDequeue(out var action))
        {
            // An exception must not reach the ENCY message loop
            try
            {
                action();
            }
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
            }
        }
        return IntPtr.Zero;
    }

    private delegate IntPtr WindowProcedure(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct WindowClass
    {
        public uint Style;
        public IntPtr WindowProcedure;
        public int ClassExtra;
        public int WindowExtra;
        public IntPtr Instance;
        public IntPtr Icon;
        public IntPtr Cursor;
        public IntPtr Background;
        public string? MenuName;
        public string ClassName;
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern ushort RegisterClass(ref WindowClass windowClass);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool UnregisterClass(string className, IntPtr instance);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr CreateWindowEx(int exStyle, string className, string windowName, int style,
        int x, int y, int width, int height, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [DllImport("user32.dll")]
    private static extern bool DestroyWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr DefWindowProc(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);
}
