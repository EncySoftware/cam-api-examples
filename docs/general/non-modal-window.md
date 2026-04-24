# Non-Modal WPF Window in an ENCY Plugin

A non-modal window is a WPF panel that opens when the user clicks a toolbar button and stays on screen while the user continues working in ENCY. The panel can read and drive ENCY live — switching views, editing parameters, reacting to selection.

This is the pattern ENCY users ask about most often. The canonical example is [`UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs`](../../UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs) — a viewport controller with Face / Edge / Corner buttons that rotate the ViewCube while the user keeps editing the part. Copy from it as a starting point.

---

## The four moving parts

A non-modal plugin is made of four files:

| File | Role |
|---|---|
| `ExtensionFactory.cs` | Creates the extension instance when ENCY loads the DLL. Standard boilerplate — no non-modal logic here. |
| `ExtensionXxx.cs` | `IExtension, IExtensionUtility, IExtensionLazyUnloadable`. Handles `Run()` and `CanUnload`. |
| `XxxWindow.xaml` / `XxxWindow.xaml.cs` | A WPF `Window` that also implements `IDisposable` and owns any COM wrappers it uses. |
| `*.settings.json` | Declares the extension to ENCY. |

`IExtensionLazyUnloadable` is the crucial piece. Without it ENCY may unload the plugin DLL while the window is still on screen — the next click crashes the host.

---

## What has to work correctly

1. **STA thread** — WPF requires STA; ENCY's .NET bridge runs MTA. The helper `WindowHelper.ShowStaWindow` spins up a dedicated STA thread and runs a `Dispatcher` loop on it.
2. **Window parenting** — the window should sit on top of the ENCY main window, and minimize/restore with it. That requires a `WindowInteropHelper` with the ENCY main window's HWND as owner. `ShowStaWindow` does this for you if you give it the HWND.
3. **COM wrapper ownership transfer** — any `ComWrapper<T>` you need inside the window must **not** be `using`-scoped in `Run()`. Construct it, hand it to the window's constructor, and dispose it in the window's `Dispose()`. `Run()` returns immediately — if you `using` the wrapper, it gets disposed before the user clicks anything.
4. **`CanUnload` flag** — return `false` while the window lives, flip to `true` in the `onClosed` callback.
5. **`window.Dispose()` on close** — `ShowStaWindow` calls it for you. Dispose every COM wrapper the window holds.

---

## Minimal skeleton — manual approach

Follows `ExtensionManageView.cs` verbatim. Copy this if you want to see every moving piece.

```csharp
using CAMAPI.Application;
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

public class MyExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private bool _canUnload;

    public IExtensionInfo? Info { get; set; }

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var applicationCom         = ComWrapper.Create(context.CamApplication);
            using var applicationMainFormCom = applicationCom.MainForm();
            var mainWindowHandle             = applicationMainFormCom.MainWindowHandle();

            // NOTE: no 'using' — ownership transfers to the window.
            var viewPortCom = applicationMainFormCom.MainViewPort();

            WindowHelper.ShowStaWindow(
                mainWindowHandle,
                createWindow: () => new MyWindow(viewPortCom),
                onClosed:     () => _canUnload = true);
            // Run returns immediately; window lives on its own STA thread.
        }
        catch (Exception e)
        {
            resultStatus.Code        = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }

    /// <summary>CanUnload returns true only after the window has been closed.</summary>
    public bool CanUnload
    {
        get => _canUnload;
        set { }
    }
}
```

The window must implement `IDisposable` and dispose every COM wrapper it was handed:

```csharp
public partial class MyWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiViewPort> _viewPortCom;

    public MyWindow(ComWrapper<ICamApiViewPort> viewPortCom)
    {
        _viewPortCom = viewPortCom; // takes ownership — do NOT dispose twice
        InitializeComponent();
    }

    private void Face_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tagStr }) return;
        if (!Enum.TryParse<TViewCubeRotateMode>(tagStr, out var mode)) return;

        try
        {
            _viewPortCom.ZoomAll(true);          // helper extension
            using var cubeCom = _viewPortCom.GetCube();
            cubeCom.Rotate(mode);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public void Dispose() => _viewPortCom.Dispose();
}
```

`WindowHelper.ShowStaWindow` calls `window.Dispose()` automatically when the window's `Closed` event fires, then invokes `onClosed`.

---

## Minimal skeleton — helper approach

`ExtensionWindowLazyUnloadable` from `CAMAPI.DotnetHelper` wraps the HWND + `ShowStaWindow` + `_canUnload` bookkeeping into one object. Same result, less boilerplate:

```csharp
public class MyExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private readonly ExtensionWindowLazyUnloadable _windowManager = new();

    public IExtensionInfo? Info { get; set; }

    public bool CanUnload
    {
        get => _windowManager.CanUnload;
        set { }
    }

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            _windowManager.SetOwnerHandle(applicationCom);

            using var mainFormCom = applicationCom.MainForm();
            var viewPortCom = mainFormCom.MainViewPort();   // again, no 'using'

            _windowManager.ShowWindow(() => new MyWindow(viewPortCom));
        }
        catch (Exception e)
        {
            resultStatus.Code        = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```

Prefer this form when the extension has only one non-modal window.

---

## `WindowHelper.ShowStaWindow` signature

```csharp
public static void ShowStaWindow<TWindow>(
    long ownerHandle,                        // HWND of the ENCY main window
    Func<TWindow> createWindow,              // called on the STA thread
    Action? onClosed = null,                 // called after window.Dispose() + dispatcher shutdown
    Action<Exception>? onException = null)   // fires if an exception is raised on the STA thread
    where TWindow : Window, IDisposable
```

- Sets `ComWrapperSettings.ApplicationApartmentState = ApartmentState.STA` on entry.
- Starts a new background STA thread and runs `Dispatcher.Run()` inside it.
- On the `Closed` event: `window.Dispose()`, then `Dispatcher.InvokeShutdown()`, then `onClosed?.Invoke()`.
- Exceptions thrown on the STA thread are routed to `onException` (or shown in a `MessageBox` if no handler was supplied).

---

## Common mistakes

- **Wrapping the transferred COM in `using`** — it gets disposed the moment `Run()` returns, and the window clicks onto a dead reference. Build it without `using`; the window's `Dispose()` is the single point of release.
- **Not implementing `IExtensionLazyUnloadable`** — ENCY may unload the DLL while the window is on screen; the next event crashes the host.
- **Returning `CanUnload = true` before the window is closed** — same outcome.
- **Calling COM from the UI thread without `Invoke`** — the COM objects were created in MTA; direct access from the STA thread is unsafe. Always go through `ComWrapper.Invoke` / extension methods, never through `.Instance` / `.It`.
- **Disposing `viewPortCom` both in `Run()` (with `using`) and in the window's `Dispose()`** — double-release. Keep disposal in the window only.

---

## Settings JSON and factory

The plugin registration is identical to any other utility extension. `*.settings.json`:

```json
{
    "name": "Utility to manage main view window by C#",
    "description": "Utility to manage main view window by C#",
    "module_path": "${extensionJsonFolder}\\MyExtension.dll",
    "extensions": [
        {
            "Utility": {
                "name": "Utility to manage main view window by C#",
                "id": "Extension.UI.MyView.Net"
            }
        }
    ]
}
```

`ExtensionFactory.cs` — standard boilerplate, no non-modal-specific code:

```csharp
namespace CAMAPI;

using Extensions;
using ResultStatus;

public class ExtensionFactory : IExtensionFactory
{
    public void OnLibraryRegistered(IExtensionFactoryContext context, out TResultStatus ret)   { ret = default; }
    public void OnLibraryUnRegistered(IExtensionFactoryContext context, out TResultStatus ret) { ret = default; }

    public IExtension? Create(string extensionIdent, out TResultStatus ret)
    {
        ret = default;
        try
        {
            if (extensionIdent == "Extension.UI.MyView.Net")
                return new MyExtension();
            throw new Exception("Unknown extension identifier: " + extensionIdent);
        }
        catch (Exception e)
        {
            ret.Code        = TResultStatusCode.rsError;
            ret.Description = e.Message;
            return null;
        }
    }
}
```

---

## Where to read next

- Full example project: [`UI/ExtensionManageViewNet/`](../../UI/ExtensionManageViewNet/) — `.xaml` + `.xaml.cs` + `Extension…View.cs` + `ExtensionFactory.cs` + `*.settings.json`.
- Threading rules and COM lifetime: [`com-lifetime.md`](com-lifetime.md) — why `Invoke` / `InvokeAndWrap` / `.IsNull` are mandatory and why `.Instance` / `.It` / `AsInstanceOf` are not used.
- Other UI patterns (inspector dialog, modal WPF window): [`ui-patterns.md`](ui-patterns.md).
