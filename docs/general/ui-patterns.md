# UI Patterns for ENCY .NET Plugins

ENCY plugins run inside a Delphi host that uses VCL for its own UI and COM for the .NET bridge. This creates threading constraints that determine which UI pattern is appropriate. There are three distinct patterns, each suited to different use cases.

---

## Pattern 1: Inspector Dialog (Recommended for Parameter Input)

The inspector dialog uses `CamApiInspectorWindow` and `SimplePropIterator` from the `CAMAPI.UIDialogs` package. It renders a native ENCY-style property grid that integrates visually with the host application. No WPF or XAML is required.

Reference example: `UI/ExtensionUtilityDialogWindowNet/project/main/ExtensionUtilityDialogWindow.cs`

### When to Use

- Collecting parameters from the user before running an operation (operation setup, export settings, etc.)
- Any scenario where you want the dialog to look and feel like a native ENCY dialog

### Complete Example

```csharp
using CAMAPI.UIDialogs;
using CAMAPI.UIDialogs.DotnetHelper;

public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
{
    resultStatus = default;
    try
    {
        // Container for the values you want to collect
        var values = new MyDialogValues();

        // Create the inspector window and property iterator
        using var window = new CamApiInspectorWindow();
        using var props = new SimplePropIterator();

        // --- String property ---
        props.AddStringProp(
            "Output file name",
            () => values.FileName,
            v => values.FileName = v);

        // --- Enum with integer index ---
        props.AddEnumIndexedProp(
            "Tolerance",
            () => values.ToleranceIndex,
            v => values.ToleranceIndex = v,
            list =>
            {
                list.Add("Coarse",  "");
                list.Add("Normal",  "");
                list.Add("Fine",    "");
            });

        // --- Enum with string ID ---
        props.AddEnumIdProp(
            "Output format",
            () => values.FormatId,
            v => values.FormatId = v,
            list =>
            {
                list.Add("step", "STEP (*.step)", "");
                list.Add("igs",  "IGES (*.igs)",  "");
                list.Add("stl",  "STL (*.stl)",   "");
            });

        // --- Nested group ---
        var advancedGroup = props.AddComplexProp("Advanced");
        advancedGroup.AddStringProp(
            "Custom postprocessor path",
            () => values.PostProcessorPath,
            v => values.PostProcessorPath = v);

        // Attach properties to the window
        window.SetPropIterator(props);

        // Configure buttons
        window.SetButtons(MessageBoxHelper.BuildButtons(
            TUIButtonType.btOk,
            TUIButtonType.btCancel));

        // Show and handle the result
        switch (window.Show())
        {
            case TUIButtonType.btOk:
                RunWithValues(context, values);
                break;

            case TUIButtonType.btCancel:
                // User cancelled — not an error; just return rsOk with no action
                break;
        }
    }
    catch (Exception e)
    {
        resultStatus.Code = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
    }
}

private class MyDialogValues
{
    public string FileName        { get; set; } = "output";
    public int    ToleranceIndex  { get; set; } = 1;   // "Normal"
    public string FormatId        { get; set; } = "step";
    public string PostProcessorPath { get; set; } = "";
}
```

### SimplePropIterator API Reference

| Method | Purpose |
|---|---|
| `AddStringProp(label, getter, setter)` | Text input field |
| `AddEnumIndexedProp(label, getter, setter, fillList)` | Drop-down by integer index (0-based) |
| `AddEnumIdProp(label, getter, setter, fillList)` | Drop-down by string ID |
| `AddComplexProp(label)` | Returns a sub-iterator for grouped/nested properties |

For `AddEnumIndexedProp`, `fillList` receives a list object and you call `list.Add(displayName, hint)`.
For `AddEnumIdProp`, `fillList` receives `list.Add(id, displayName, hint)`.

The `getter` and `setter` lambdas are called live as the user edits the dialog — you can read the current selection at any time from the values object after `Show()` returns.

---

## Pattern 2: Modal WPF or WinForms Window

Use a dedicated STA thread to host a WPF/WinForms dialog that the caller waits for before continuing. This is appropriate when you need a rich custom UI (forms, charts, file pickers, etc.) and the result must be available synchronously.

Reference example: `UI/ExtensionUtilityNotifyNet/project/main/ExtensionUtilityNotify.cs`

### The Threading Constraint

ENCY's .NET bridge runs on an MTA thread. WPF requires STA. You cannot `ShowDialog()` from an MTA thread. The solution is to create a dedicated STA thread, block on it, and capture the result via a closure.

```csharp
private static string? AskUserForText()
{
    string? result = null;

    var thread = new Thread(() =>
    {
        var window = new TextInputWindow();
        if (window.ShowDialog() == true)
            result = window.UserInput;
        // Window is destroyed and STA dispatcher stops here
    });

    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    thread.Join(); // Block until the window closes

    return result; // Safe to read after Join()
}
```

Full usage inside a `Run` method:

```csharp
public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
{
    resultStatus = default;
    try
    {
        var userText = AskUserForText();
        if (userText == null)
            return; // User cancelled

        UIDialogs.Notify(TLogEventType.leInfo, "My Plugin", $"You entered: {userText}");
    }
    catch (Exception e)
    {
        resultStatus.Code = TResultStatusCode.rsError;
        resultStatus.Description = e.Message;
    }
}
```

### Critical Caveat: No COM Access Inside the STA Thread

COM objects obtained from the ENCY API **cannot** be accessed directly inside the STA thread's lambda. COM apartment rules prohibit cross-apartment access without proper marshalling. If you need to display data retrieved from COM in the dialog, read it **before** spawning the STA thread and pass it as plain .NET values:

```csharp
// Read COM data on the calling (MTA) thread
var operationNames = GetOperationNames(context); // returns List<string>

// Pass plain data to the STA thread
string? selected = null;
var thread = new Thread(() =>
{
    var window = new PickOperationWindow(operationNames); // receives List<string>
    if (window.ShowDialog() == true)
        selected = window.SelectedName;
});
thread.SetApartmentState(ApartmentState.STA);
thread.Start();
thread.Join();
```

---

## Pattern 3: Non-Modal WPF Window

Use a non-modal window when you want to display a persistent panel (viewport controller, live data viewer, etc.) that stays open while the user continues working in ENCY.

Reference example: `UI/ExtensionManageViewNet/project/main/ExtensionManageView.cs`

This pattern requires implementing `IExtensionLazyUnloadable` so ENCY knows not to unload the plugin DLL while the window is open.

### IExtensionLazyUnloadable

When ENCY wants to unload an extension, it checks `IExtensionLazyUnloadable.CanUnload`. Return `false` to defer unloading. Set it to `true` (from the window-closed callback) when the window closes.

### Using ExtensionWindowLazyUnloadable (Recommended)

`ExtensionWindowLazyUnloadable` from `CAMAPI.DotnetHelper` implements `IExtensionLazyUnloadable` and wraps `WindowHelper.ShowStaWindow`, handling the `CanUnload` flag automatically:

```csharp
using CAMAPI.DotnetHelper;
using CAMAPI.Extensions;
using CAMAPI.ResultStatus;

public class MyExtension : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private readonly ExtensionWindowLazyUnloadable _windowManager = new();

    public IExtensionInfo? Info { get; set; }

    // Forward IExtensionLazyUnloadable.CanUnload to the helper
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

            // Get the owner window handle for parenting
            _windowManager.SetOwnerHandle(applicationCom);

            // Obtain a COM object to pass into the window.
            // Note: DO NOT wrap in 'using' here — transfer ownership to the window
            var viewPortCom = applicationCom.InvokeAndWrap(app =>
                app.MainForm).InvokeAndWrap(f => f.MainViewPort);

            // Show the window on a background STA thread.
            // The window's constructor receives the COM wrapper and owns its lifetime.
            // The window must implement IDisposable and call viewPortCom.Dispose() there.
            _windowManager.ShowWindow(
                () => new MyViewWindow(viewPortCom));
            // Run returns immediately; the window lives on its own thread
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```

### Using WindowHelper Directly

If you need more control, use `WindowHelper.ShowStaWindow` directly:

```csharp
public class ExtensionManageView : IExtension, IExtensionUtility, IExtensionLazyUnloadable
{
    private bool _canUnload;

    public IExtensionInfo? Info { get; set; }
    public bool CanUnload { get => _canUnload; set { } }

    public void Run(IExtensionUtilityContext context, out TResultStatus resultStatus)
    {
        resultStatus = default;
        try
        {
            using var applicationCom = ComWrapper.Create(context.CamApplication);
            using var mainFormCom = applicationCom.InvokeAndWrap(app => app.MainForm);
            var ownerHandle = mainFormCom.MainWindowHandle();

            // Transfer ownership of the view port COM wrapper to the window
            var viewPortCom = mainFormCom.InvokeAndWrap(f => f.MainViewPort);

            WindowHelper.ShowStaWindow(
                ownerHandle,
                createWindow: () => new ViewControlWindow(viewPortCom),
                onClosed:     () => _canUnload = true);
            // Run returns here; window is alive on its STA thread
        }
        catch (Exception e)
        {
            resultStatus.Code = TResultStatusCode.rsError;
            resultStatus.Description = e.Message;
        }
    }
}
```

`WindowHelper.ShowStaWindow` signature:

```csharp
public static void ShowStaWindow<TWindow>(
    long ownerHandle,        // Win32 HWND of the ENCY main window (for parenting)
    Func<TWindow> createWindow,     // called on the STA thread to construct the window
    Action? onClosed = null,        // called after window.Dispose() and dispatcher shutdown
    Action<Exception>? onException = null) // called if an exception occurs on the STA thread
    where TWindow : Window, IDisposable
```

`WindowHelper.ShowStaWindow` also sets `ComWrapperSettings.ApplicationApartmentState = ApartmentState.STA` automatically.

### Window Lifetime and COM Disposal

The window class must implement `IDisposable`. Any COM wrappers that were passed to the window and are not owned elsewhere must be disposed there:

```csharp
public class ViewControlWindow : Window, IDisposable
{
    private readonly ComWrapper<ICamApiViewPort> _viewPortCom;

    public ViewControlWindow(ComWrapper<ICamApiViewPort> viewPortCom)
    {
        _viewPortCom = viewPortCom; // takes ownership
        InitializeComponent();
    }

    public void Dispose()
    {
        _viewPortCom.Dispose();
    }
}
```

`WindowHelper.ShowStaWindow` calls `window.Dispose()` automatically when the window's `Closed` event fires.

### CanUnload Behaviour

- While the window is open: `CanUnload` returns `false`. ENCY defers plugin unloading.
- After the window closes (the `onClosed` callback fires): `CanUnload` returns `true`. ENCY may now unload the plugin DLL.

If `IExtensionLazyUnloadable` is not implemented, ENCY may unload the DLL while the window is still on screen, causing an immediate crash.

---

## UIDialogs Static Helpers

`UIDialogs` (in `CAMAPI.UIDialogs.DotnetHelper`) provides lightweight helper methods for common UI interactions without requiring a full window:

```csharp
using CAMAPI.UIDialogs.DotnetHelper;
using CAMAPI.Logger;

// Show a notification popup (info, warning, or error)
UIDialogs.Notify(TLogEventType.leInfo,    "My Plugin", "Toolpath calculated successfully.");
UIDialogs.Notify(TLogEventType.leWarning, "My Plugin", "No geometry was selected.");
UIDialogs.Notify(TLogEventType.leError,   "My Plugin", "File not found: output.nc");
```

For message boxes with button choices and for file/folder selection dialogs, obtain a helper instance:

```csharp
using var helper = UIDialogs.CreateHelper();

// Message box with OK / Cancel
var result = helper.ShowMessageBox(
    "Confirm",
    "This will overwrite existing toolpaths. Continue?",
    MessageBoxHelper.BuildButtons(TUIButtonType.btOk, TUIButtonType.btCancel));

if (result == TUIButtonType.btCancel)
    return;

// Open file dialog
var filePath = helper.ShowOpenFileDialog(
    "Select postprocessor",
    "ENCY postprocessor (*.sppx)|*.sppx|All files (*.*)|*.*");

if (string.IsNullOrEmpty(filePath))
    return;
```

`UIDialogs.CreateHelper()` returns an `IDisposable`; always use `using`.
